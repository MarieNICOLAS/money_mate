using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
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
