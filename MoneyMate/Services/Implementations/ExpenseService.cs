using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Models.DTOs;
using MoneyMate.Services.Common;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour la gestion des dépenses.
    /// </summary>
    public class ExpenseService : IExpenseService
    {
        private readonly IMoneyMateDbContext _dbContext;

        public ExpenseService()
            : this(DbContextFactory.CreateDefault())
        {
        }

        public ExpenseService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public Task<ServiceResult<List<Expense>>> GetExpensesAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<Expense>>.Failure(
                            "EXPENSE_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    List<Expense> expenses = _dbContext.GetExpensesByUserId(userId);
                    return ServiceResult<List<Expense>>.Success(expenses);
                },
                operationName: nameof(GetExpensesAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des dépenses.");
        }

        public Task<IReadOnlyList<ExpenseListItemDto>> GetExpensesAsync(int userId, ExpenseFilterDto filter)
        {
            return Task.Run<IReadOnlyList<ExpenseListItemDto>>(() =>
            {
                if (userId <= 0)
                    return [];

                filter ??= new ExpenseFilterDto();

                Dictionary<int, Category> categoriesById = _dbContext.GetCategoriesByUserId(userId)
                    .GroupBy(category => category.Id)
                    .Select(group => group.First())
                    .ToDictionary(category => category.Id, category => category);

                IEnumerable<Expense> expenses = ApplyExpenseFilter(
                    _dbContext.GetExpensesByUserId(userId),
                    ToLegacyFilter(userId, filter));

                IEnumerable<ExpenseListItemDto> items = expenses
                    .Select(expense => MapExpenseListItem(expense, categoriesById));

                items = ApplyDtoFilter(items, filter);
                items = ApplyDtoSort(items, filter);

                return items.Take(250).ToList();
            });
        }

        public async Task<ExpenseSummaryDto> GetExpenseSummaryAsync(int userId, ExpenseFilterDto filter)
        {
            if (userId <= 0)
                return new ExpenseSummaryDto();

            filter ??= new ExpenseFilterDto();
            ExpenseFilterDto periodFilter = filter.Clone();
            periodFilter.SearchText = string.Empty;

            IReadOnlyList<ExpenseListItemDto> currentItems = await GetExpensesAsync(userId, periodFilter);
            DateTime currentStart = periodFilter.StartDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime currentEnd = periodFilter.EndDate ?? currentStart.AddMonths(1).AddDays(-1);
            int daySpan = Math.Max(1, (currentEnd.Date - currentStart.Date).Days + 1);

            ExpenseFilterDto previousFilter = periodFilter.Clone();
            previousFilter.StartDate = currentStart.AddDays(-daySpan);
            previousFilter.EndDate = currentStart.AddDays(-1);
            IReadOnlyList<ExpenseListItemDto> previousItems = await GetExpensesAsync(userId, previousFilter);

            decimal totalExpenses = currentItems.Where(item => item.IsExpense || item.IsFixedCharge).Sum(item => item.Amount);
            decimal totalIncome = currentItems.Where(item => item.IsIncome).Sum(item => item.Amount);
            decimal previousExpenses = previousItems.Where(item => item.IsExpense || item.IsFixedCharge).Sum(item => item.Amount);

            decimal variation = previousExpenses == 0m
                ? totalExpenses == 0m ? 0m : 100m
                : Math.Round(((totalExpenses - previousExpenses) / previousExpenses) * 100m, 1);

            return new ExpenseSummaryDto
            {
                TotalExpenses = totalExpenses,
                TotalIncome = totalIncome,
                Balance = totalIncome - totalExpenses,
                PreviousMonthVariationPercent = variation,
                VariationLabel = $"{(variation > 0 ? "+" : string.Empty)}{variation:0.#} %",
                VariationColor = variation <= 0 ? "#5CB85C" : "#D9534F",
                TopCategories = BuildTopCategories(currentItems, totalExpenses)
            };
        }

        public Task<ServiceResult<List<Expense>>> SearchExpensesAsync(ExpenseFilter filter)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(filter);

                    if (filter.UserId <= 0)
                        return ServiceResult<List<Expense>>.Failure(
                            "EXPENSE_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    if (filter.StartDate.HasValue &&
                        filter.EndDate.HasValue &&
                        filter.StartDate.Value > filter.EndDate.Value)
                    {
                        return ServiceResult<List<Expense>>.Failure(
                            "EXPENSE_INVALID_PERIOD",
                            "La période demandée est invalide.");
                    }

                    if (filter.MinAmount.HasValue &&
                        filter.MaxAmount.HasValue &&
                        filter.MinAmount.Value > filter.MaxAmount.Value)
                    {
                        return ServiceResult<List<Expense>>.Failure(
                            "EXPENSE_INVALID_AMOUNT_RANGE",
                            "La plage de montants demandée est invalide.");
                    }

                    IEnumerable<Expense> expenses = ApplyExpenseFilter(
                        _dbContext.GetExpensesByUserId(filter.UserId),
                        filter);

                    if (filter.Skip > 0)
                        expenses = expenses.Skip(filter.Skip);

                    if (filter.Take > 0)
                        expenses = expenses.Take(filter.Take);

                    return ServiceResult<List<Expense>>.Success(expenses.ToList());
                },
                operationName: nameof(SearchExpensesAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la recherche des dépenses.");
        }

        public Task<ServiceResult<List<Expense>>> GetExpensesByCategoryAsync(int userId, int categoryId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0 || categoryId <= 0)
                        return ServiceResult<List<Expense>>.Failure(
                            "EXPENSE_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    List<Expense> expenses = _dbContext.GetExpensesByCategory(userId, categoryId);
                    return ServiceResult<List<Expense>>.Success(expenses);
                },
                operationName: nameof(GetExpensesByCategoryAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des dépenses.");
        }

        public Task<ServiceResult<List<Expense>>> GetExpensesByPeriodAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0 || startDate > endDate)
                        return ServiceResult<List<Expense>>.Failure(
                            "EXPENSE_INVALID_INPUT",
                            "La période demandée est invalide.");

                    List<Expense> expenses = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.DateOperation >= startDate && expense.DateOperation <= endDate)
                        .ToList();

                    return ServiceResult<List<Expense>>.Success(expenses);
                },
                operationName: nameof(GetExpensesByPeriodAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des dépenses.");
        }

        public Task<ServiceResult<Expense>> GetExpenseByIdAsync(int expenseId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (expenseId <= 0 || userId <= 0)
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    Expense? expense = _dbContext.GetExpenseById(expenseId, userId);
                    if (expense is null)
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_NOT_FOUND",
                            "Dépense introuvable.");

                    return ServiceResult<Expense>.Success(expense);
                },
                operationName: nameof(GetExpenseByIdAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement de la dépense.");
        }

        public Task<ServiceResult<Expense>> CreateExpenseAsync(Expense expense)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(expense);

                    ServiceResult validationResult = ValidateExpense(expense);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<Expense>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);

                    if (!HasActiveBudgetForDate(expense.UserId, expense.DateOperation))
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_BUDGET_REQUIRED",
                            "Un budget actif est requis pour la date de cette dépense.");
                    }

                    if (!CategoryExistsForUser(expense.UserId, expense.CategoryId))
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_CATEGORY_NOT_FOUND",
                            "La catégorie sélectionnée est introuvable ou inactive.");
                    }

                    expense.Note = expense.Note?.Trim() ?? string.Empty;

                    int expenseId = _dbContext.InsertExpense(expense);
                    if (expenseId <= 0)
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_CREATE_FAILED",
                            "Impossible de créer la dépense.");
                    }

                    expense.Id = expenseId;
                    return ServiceResult<Expense>.Success(expense, "Dépense créée avec succès.");
                },
                operationName: nameof(CreateExpenseAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la création de la dépense.");
        }

        public Task<ServiceResult<Expense>> UpdateExpenseAsync(Expense expense)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(expense);

                    if (expense.Id <= 0)
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_INVALID_ID",
                            "La dépense à modifier est invalide.");
                    }

                    ServiceResult validationResult = ValidateExpense(expense);
                    if (!validationResult.IsSuccess)
                    {
                        return ServiceResult<Expense>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);
                    }

                    Expense? existingExpense = _dbContext.GetExpenseById(expense.Id, expense.UserId);
                    if (existingExpense is null)
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_NOT_FOUND",
                            "Dépense introuvable.");
                    }

                    if (!CategoryExistsForUser(expense.UserId, expense.CategoryId))
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_CATEGORY_NOT_FOUND",
                            "La catégorie sélectionnée est introuvable ou inactive.");
                    }

                    expense.Note = expense.Note?.Trim() ?? string.Empty;

                    int updatedRows = _dbContext.UpdateExpense(expense);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_UPDATE_FAILED",
                            "La mise à jour de la dépense a échoué.");
                    }

                    return ServiceResult<Expense>.Success(expense, "Dépense mise à jour avec succès.");
                },
                operationName: nameof(UpdateExpenseAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour de la dépense.");
        }

        public Task<ServiceResult> DeleteExpenseAsync(int expenseId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (expenseId <= 0 || userId <= 0)
                        return ServiceResult.Failure(
                            "EXPENSE_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    Expense? expense = _dbContext.GetExpenseById(expenseId, userId);
                    if (expense is null)
                        return ServiceResult.Failure(
                            "EXPENSE_NOT_FOUND",
                            "Dépense introuvable.");

                    int deletedRows = _dbContext.DeleteExpense(expense);
                    if (deletedRows != 1)
                    {
                        return ServiceResult.Failure(
                            "EXPENSE_DELETE_FAILED",
                            "La suppression de la dépense a échoué.");
                    }

                    return ServiceResult.Success("Dépense supprimée avec succès.");
                },
                operationName: nameof(DeleteExpenseAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la suppression de la dépense.");
        }

        public Task<ServiceResult<decimal>> GetTotalExpensesAsync(
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? categoryId = null)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<decimal>.Failure(
                            "EXPENSE_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    if (startDate.HasValue &&
                        endDate.HasValue &&
                        startDate.Value > endDate.Value)
                    {
                        return ServiceResult<decimal>.Failure(
                            "EXPENSE_INVALID_PERIOD",
                            "La période demandée est invalide.");
                    }

                    IEnumerable<Expense> expenses = ApplyExpenseFilter(
                        _dbContext.GetExpensesByUserId(userId),
                        new ExpenseFilter
                        {
                            UserId = userId,
                            StartDate = startDate,
                            EndDate = endDate,
                            CategoryId = categoryId
                        });

                    decimal total = expenses.Sum(expense => expense.Amount);
                    return ServiceResult<decimal>.Success(total);
                },
                operationName: nameof(GetTotalExpensesAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du calcul des dépenses.");
        }

        public Task<ServiceResult<int>> GetExpensesCountAsync(
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? categoryId = null,
            bool? isFixedCharge = null)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<int>.Failure(
                            "EXPENSE_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    if (startDate.HasValue &&
                        endDate.HasValue &&
                        startDate.Value > endDate.Value)
                    {
                        return ServiceResult<int>.Failure(
                            "EXPENSE_INVALID_PERIOD",
                            "La période demandée est invalide.");
                    }

                    int count = ApplyExpenseFilter(
                            _dbContext.GetExpensesByUserId(userId),
                            new ExpenseFilter
                            {
                                UserId = userId,
                                StartDate = startDate,
                                EndDate = endDate,
                                CategoryId = categoryId,
                                IsFixedCharge = isFixedCharge
                            })
                        .Count();

                    return ServiceResult<int>.Success(count);
                },
                operationName: nameof(GetExpensesCountAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du calcul du nombre de dépenses.");
        }

        public Task<ServiceResult<Expense>> DuplicateExpenseAsync(int expenseId, int userId, DateTime? newDate = null)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (expenseId <= 0 || userId <= 0)
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_INVALID_INPUT",
                            ServiceMessages.InvalidInput);
                    }

                    Expense? existingExpense = _dbContext.GetExpenseById(expenseId, userId);
                    if (existingExpense is null)
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_NOT_FOUND",
                            "Dépense introuvable.");
                    }

                    Expense duplicatedExpense = new()
                    {
                        UserId = existingExpense.UserId,
                        CategoryId = existingExpense.CategoryId,
                        Amount = existingExpense.Amount,
                        Note = existingExpense.Note,
                        IsFixedCharge = existingExpense.IsFixedCharge,
                        DateOperation = newDate ?? DateTime.Now
                    };

                    ServiceResult validationResult = ValidateExpense(duplicatedExpense);
                    if (!validationResult.IsSuccess)
                    {
                        return ServiceResult<Expense>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);
                    }

                    if (!HasActiveBudgetForDate(duplicatedExpense.UserId, duplicatedExpense.DateOperation))
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_BUDGET_REQUIRED",
                            "Un budget actif est requis pour la date de cette dépense.");
                    }

                    if (!CategoryExistsForUser(duplicatedExpense.UserId, duplicatedExpense.CategoryId))
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_CATEGORY_NOT_FOUND",
                            "La catégorie sélectionnée est introuvable ou inactive.");
                    }

                    duplicatedExpense.Note = duplicatedExpense.Note?.Trim() ?? string.Empty;

                    int duplicatedExpenseId = _dbContext.InsertExpense(duplicatedExpense);
                    if (duplicatedExpenseId <= 0)
                    {
                        return ServiceResult<Expense>.Failure(
                            "EXPENSE_DUPLICATE_FAILED",
                            "Impossible de dupliquer la dépense.");
                    }

                    duplicatedExpense.Id = duplicatedExpenseId;
                    return ServiceResult<Expense>.Success(duplicatedExpense, "Dépense dupliquée avec succès.");
                },
                operationName: nameof(DuplicateExpenseAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la duplication de la dépense.");
        }

        public Task<ServiceResult<int>> MigrateExpenseCategoryAsync(int userId, int sourceCategoryId, int targetCategoryId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0 || sourceCategoryId <= 0 || targetCategoryId <= 0)
                    {
                        return ServiceResult<int>.Failure(
                            "EXPENSE_INVALID_INPUT",
                            ServiceMessages.InvalidInput);
                    }

                    if (!CategoryExistsForUser(userId, targetCategoryId))
                    {
                        return ServiceResult<int>.Failure(
                            "EXPENSE_CATEGORY_NOT_FOUND",
                            "La catégorie cible est introuvable ou inactive.");
                    }

                    int updatedRows = _dbContext.MigrateCategoryUsageForUser(userId, sourceCategoryId, targetCategoryId);
                    return ServiceResult<int>.Success(updatedRows);
                },
                operationName: nameof(MigrateExpenseCategoryAsync),
                fallbackErrorCode: "EXPENSE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la migration des dépenses.");
        }

        private static ServiceResult ValidateExpense(Expense expense)
        {
            if (expense.UserId <= 0)
                return ServiceResult.Failure("EXPENSE_INVALID_USER", ServiceMessages.InvalidUser);

            if (expense.CategoryId <= 0)
                return ServiceResult.Failure("EXPENSE_INVALID_CATEGORY", "Catégorie invalide.");

            if (!ValidationHelper.IsValidAmount(expense.Amount))
                return ServiceResult.Failure("EXPENSE_INVALID_AMOUNT", "Le montant doit être strictement positif.");

            if (!ValidationHelper.IsValidDate(expense.DateOperation))
                return ServiceResult.Failure("EXPENSE_INVALID_DATE", "La date de la dépense ne peut pas être dans le futur.");

            return ServiceResult.Success();
        }

        private bool HasActiveBudgetForDate(int userId, DateTime expenseDate)
        {
            DateTime targetDate = expenseDate.Date;

            return _dbContext.GetBudgetsByUserId(userId)
                .Where(budget => budget.IsActive)
                .Any(budget =>
                {
                    DateTime startDate = new DateTime(budget.StartDate.Year, budget.StartDate.Month, 1);
                    DateTime endDate = (budget.EndDate ?? startDate.AddMonths(1).AddDays(-1)).Date;
                    return targetDate >= startDate.Date && targetDate <= endDate;
                });
        }

        private static ExpenseFilter ToLegacyFilter(int userId, ExpenseFilterDto filter)
            => new()
            {
                UserId = userId,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                CategoryId = filter.CategoryIds.Count == 1 ? filter.CategoryIds[0] : null,
                IsFixedCharge = filter.IsFixedCharge,
                MinAmount = filter.MinAmount,
                MaxAmount = filter.MaxAmount,
                SearchTerm = string.Empty
            };

        private static ExpenseListItemDto MapExpenseListItem(Expense expense, IReadOnlyDictionary<int, Category> categoriesById)
        {
            categoriesById.TryGetValue(expense.CategoryId, out Category? category);

            string type = ResolveOperationType(expense, category);
            bool isIncome = string.Equals(type, "Revenu", StringComparison.OrdinalIgnoreCase);
            bool isTransfer = string.Equals(type, "Transfert", StringComparison.OrdinalIgnoreCase);
            string title = string.IsNullOrWhiteSpace(expense.Note) ? category?.Name ?? "Opération" : expense.Note.Trim();

            return new ExpenseListItemDto
            {
                Id = expense.Id,
                CategoryId = expense.CategoryId,
                Title = title,
                CategoryName = category?.Name ?? "Catégorie",
                Note = expense.Note ?? string.Empty,
                Amount = Math.Abs(expense.Amount),
                FormattedAmount = CurrencyHelper.Format(Math.Abs(expense.Amount)),
                OperationDate = expense.DateOperation.Date,
                FormattedDate = expense.DateOperation.ToString("dd/MM/yyyy"),
                Type = type,
                IsIncome = isIncome,
                IsExpense = !isIncome && !isTransfer,
                IsTransfer = isTransfer,
                IsFixedCharge = expense.IsFixedCharge,
                Icon = string.IsNullOrWhiteSpace(category?.Icon) ? "💰" : category!.Icon,
                IconBackgroundColor = LightenColor(category?.Color),
                AmountColor = isIncome ? "#5CB85C" : isTransfer ? "#6B7A8F" : "#D9534F",
                Devise = "EUR"
            };
        }

        private static IEnumerable<ExpenseListItemDto> ApplyDtoFilter(IEnumerable<ExpenseListItemDto> items, ExpenseFilterDto filter)
        {
            if (filter.CategoryIds.Count > 1)
                items = items.Where(item => filter.CategoryIds.Contains(item.CategoryId));

            if (!string.IsNullOrWhiteSpace(filter.OperationType) && !string.Equals(filter.OperationType, "Toutes", StringComparison.OrdinalIgnoreCase))
                items = items.Where(item => string.Equals(item.Type, SingularizeOperationType(filter.OperationType), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string searchText = filter.SearchText.Trim();
                items = items.Where(item =>
                    item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    item.Note.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    item.CategoryName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    item.FormattedAmount.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            return items;
        }

        private static IEnumerable<ExpenseListItemDto> ApplyDtoSort(IEnumerable<ExpenseListItemDto> items, ExpenseFilterDto filter)
        {
            string sortBy = string.IsNullOrWhiteSpace(filter.SortBy) ? "Date" : filter.SortBy;

            return sortBy switch
            {
                "Montant" => filter.SortDescending
                    ? items.OrderByDescending(item => item.Amount).ThenByDescending(item => item.OperationDate)
                    : items.OrderBy(item => item.Amount).ThenByDescending(item => item.OperationDate),
                "Catégorie" => filter.SortDescending
                    ? items.OrderByDescending(item => item.CategoryName).ThenByDescending(item => item.OperationDate)
                    : items.OrderBy(item => item.CategoryName).ThenByDescending(item => item.OperationDate),
                _ => filter.SortDescending
                    ? items.OrderByDescending(item => item.OperationDate).ThenByDescending(item => item.Id)
                    : items.OrderBy(item => item.OperationDate).ThenBy(item => item.Id)
            };
        }

        private static IReadOnlyList<CategorySummaryDto> BuildTopCategories(IReadOnlyList<ExpenseListItemDto> items, decimal totalExpenses)
        {
            if (totalExpenses <= 0m)
                return [];

            string[] palette = ["#6B7A8F", "#F6B092", "#5CB85C", "#D9534F", "#8CA6B8"];

            return items
                .Where(item => item.IsExpense || item.IsFixedCharge)
                .GroupBy(item => item.CategoryName)
                .OrderByDescending(group => group.Sum(item => item.Amount))
                .Take(4)
                .Select((group, index) =>
                {
                    ExpenseListItemDto first = group.First();
                    decimal amount = group.Sum(item => item.Amount);

                    return new CategorySummaryDto
                    {
                        CategoryId = first.CategoryId,
                        Label = group.Key,
                        Amount = amount,
                        Percentage = (double)Math.Round(amount / totalExpenses * 100m, 1),
                        ColorHex = palette[index % palette.Length],
                        Icon = first.Icon
                    };
                })
                .ToList();
        }

        private static string ResolveOperationType(Expense expense, Category? category)
        {
            string haystack = $"{category?.Name} {expense.Note}".ToLowerInvariant();

            if (expense.Amount < 0 || haystack.Contains("revenu") || haystack.Contains("income") || haystack.Contains("salaire"))
                return "Revenu";

            if (haystack.Contains("transfert") || haystack.Contains("transfer") || haystack.Contains("virement interne"))
                return "Transfert";

            return "Dépense";
        }

        private static string SingularizeOperationType(string operationType)
            => operationType switch
            {
                "Dépenses" => "Dépense",
                "Revenus" => "Revenu",
                "Transferts" => "Transfert",
                _ => operationType
            };

        private static string LightenColor(string? color)
            => string.IsNullOrWhiteSpace(color) ? "#EEF2F5" : "#EEF2F5";

        private static IEnumerable<Expense> ApplyExpenseFilter(IEnumerable<Expense> expenses, ExpenseFilter filter)
        {
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
                expenses = expenses.Where(expense => expense.CategoryId == filter.CategoryId.Value);

            if (filter.StartDate.HasValue)
                expenses = expenses.Where(expense => expense.DateOperation >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                expenses = expenses.Where(expense => expense.DateOperation <= filter.EndDate.Value);

            if (filter.IsFixedCharge.HasValue)
                expenses = expenses.Where(expense => expense.IsFixedCharge == filter.IsFixedCharge.Value);

            if (filter.MinAmount.HasValue)
                expenses = expenses.Where(expense => expense.Amount >= filter.MinAmount.Value);

            if (filter.MaxAmount.HasValue)
                expenses = expenses.Where(expense => expense.Amount <= filter.MaxAmount.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                string searchTerm = filter.SearchTerm.Trim();
                expenses = expenses.Where(expense =>
                    !string.IsNullOrWhiteSpace(expense.Note) &&
                    expense.Note.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            return expenses;
        }

        private bool CategoryExistsForUser(int userId, int categoryId)
        {
            Category? category = _dbContext.GetCategoryById(categoryId, userId);
            return category is not null && category.IsActive;
        }
    }
}
