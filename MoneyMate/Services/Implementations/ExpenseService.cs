using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
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
        private readonly MoneyMateDbContext _dbContext;

        public ExpenseService()
        {
            _dbContext = DatabaseService.Instance;
        }

        public async Task<ServiceResult<List<Expense>>> GetExpensesAsync(int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<Expense>>.Failure("EXPENSE_INVALID_USER", "Utilisateur invalide.");

                    List<Expense> expenses = _dbContext.GetExpensesByUserId(userId);
                    return ServiceResult<List<Expense>>.Success(expenses);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetExpensesAsync : {ex.Message}");
                    return ServiceResult<List<Expense>>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des dépenses.");
                }
            });
        }

        public async Task<ServiceResult<List<Expense>>> SearchExpensesAsync(ExpenseFilter filter)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(filter);

                    if (filter.UserId <= 0)
                        return ServiceResult<List<Expense>>.Failure("EXPENSE_INVALID_USER", "Utilisateur invalide.");

                    if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate.Value > filter.EndDate.Value)
                        return ServiceResult<List<Expense>>.Failure("EXPENSE_INVALID_PERIOD", "La période demandée est invalide.");

                    if (filter.MinAmount.HasValue && filter.MaxAmount.HasValue && filter.MinAmount.Value > filter.MaxAmount.Value)
                        return ServiceResult<List<Expense>>.Failure("EXPENSE_INVALID_AMOUNT_RANGE", "La plage de montants demandée est invalide.");

                    IEnumerable<Expense> expenses = ApplyExpenseFilter(_dbContext.GetExpensesByUserId(filter.UserId), filter);

                    if (filter.Skip > 0)
                        expenses = expenses.Skip(filter.Skip);

                    if (filter.Take > 0)
                        expenses = expenses.Take(filter.Take);

                    return ServiceResult<List<Expense>>.Success(expenses.ToList());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur SearchExpensesAsync : {ex.Message}");
                    return ServiceResult<List<Expense>>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors de la recherche des dépenses.");
                }
            });
        }

        public async Task<ServiceResult<List<Expense>>> GetExpensesByCategoryAsync(int userId, int categoryId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0 || categoryId <= 0)
                        return ServiceResult<List<Expense>>.Failure("EXPENSE_INVALID_INPUT", "Les informations demandées sont invalides.");

                    List<Expense> expenses = _dbContext.GetExpensesByCategory(userId, categoryId);
                    return ServiceResult<List<Expense>>.Success(expenses);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetExpensesByCategoryAsync : {ex.Message}");
                    return ServiceResult<List<Expense>>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des dépenses.");
                }
            });
        }

        public async Task<ServiceResult<List<Expense>>> GetExpensesByPeriodAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0 || startDate > endDate)
                        return ServiceResult<List<Expense>>.Failure("EXPENSE_INVALID_INPUT", "La période demandée est invalide.");

                    List<Expense> expenses = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.DateOperation >= startDate && expense.DateOperation <= endDate)
                        .ToList();

                    return ServiceResult<List<Expense>>.Success(expenses);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetExpensesByPeriodAsync : {ex.Message}");
                    return ServiceResult<List<Expense>>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des dépenses.");
                }
            });
        }

        public async Task<ServiceResult<Expense>> GetExpenseByIdAsync(int expenseId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (expenseId <= 0 || userId <= 0)
                        return ServiceResult<Expense>.Failure("EXPENSE_INVALID_INPUT", "Les informations demandées sont invalides.");

                    Expense? expense = _dbContext.GetExpenseById(expenseId, userId);
                    if (expense == null)
                        return ServiceResult<Expense>.Failure("EXPENSE_NOT_FOUND", "Dépense introuvable.");

                    return ServiceResult<Expense>.Success(expense);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetExpenseByIdAsync : {ex.Message}");
                    return ServiceResult<Expense>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement de la dépense.");
                }
            });
        }

        public async Task<ServiceResult<Expense>> CreateExpenseAsync(Expense expense)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(expense);

                    ServiceResult validationResult = ValidateExpense(expense);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<Expense>.Failure(validationResult.ErrorCode, validationResult.Message);

                    if (!CategoryExistsForUser(expense.UserId, expense.CategoryId))
                        return ServiceResult<Expense>.Failure("EXPENSE_CATEGORY_NOT_FOUND", "La catégorie sélectionnée est introuvable ou inactive.");

                    expense.Note = expense.Note?.Trim() ?? string.Empty;

                    int expenseId = _dbContext.InsertExpense(expense);
                    if (expenseId <= 0)
                        return ServiceResult<Expense>.Failure("EXPENSE_CREATE_FAILED", "Impossible de créer la dépense.");

                    expense.Id = expenseId;
                    return ServiceResult<Expense>.Success(expense, "Dépense créée avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur CreateExpenseAsync : {ex.Message}");
                    return ServiceResult<Expense>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors de la création de la dépense.");
                }
            });
        }

        public async Task<ServiceResult<Expense>> UpdateExpenseAsync(Expense expense)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(expense);

                    if (expense.Id <= 0)
                        return ServiceResult<Expense>.Failure("EXPENSE_INVALID_ID", "La dépense à modifier est invalide.");

                    ServiceResult validationResult = ValidateExpense(expense);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<Expense>.Failure(validationResult.ErrorCode, validationResult.Message);

                    Expense? existingExpense = _dbContext.GetExpenseById(expense.Id, expense.UserId);
                    if (existingExpense == null)
                        return ServiceResult<Expense>.Failure("EXPENSE_NOT_FOUND", "Dépense introuvable.");

                    if (!CategoryExistsForUser(expense.UserId, expense.CategoryId))
                        return ServiceResult<Expense>.Failure("EXPENSE_CATEGORY_NOT_FOUND", "La catégorie sélectionnée est introuvable ou inactive.");

                    expense.Note = expense.Note?.Trim() ?? string.Empty;

                    int updatedRows = _dbContext.UpdateExpense(expense);
                    if (updatedRows != 1)
                        return ServiceResult<Expense>.Failure("EXPENSE_UPDATE_FAILED", "La mise à jour de la dépense a échoué.");

                    return ServiceResult<Expense>.Success(expense, "Dépense mise à jour avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur UpdateExpenseAsync : {ex.Message}");
                    return ServiceResult<Expense>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors de la mise à jour de la dépense.");
                }
            });
        }

        public async Task<ServiceResult> DeleteExpenseAsync(int expenseId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (expenseId <= 0 || userId <= 0)
                        return ServiceResult.Failure("EXPENSE_INVALID_INPUT", "Les informations demandées sont invalides.");

                    Expense? expense = _dbContext.GetExpenseById(expenseId, userId);
                    if (expense == null)
                        return ServiceResult.Failure("EXPENSE_NOT_FOUND", "Dépense introuvable.");

                    int deletedRows = _dbContext.DeleteExpense(expense);
                    if (deletedRows != 1)
                        return ServiceResult.Failure("EXPENSE_DELETE_FAILED", "La suppression de la dépense a échoué.");

                    return ServiceResult.Success("Dépense supprimée avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur DeleteExpenseAsync : {ex.Message}");
                    return ServiceResult.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors de la suppression de la dépense.");
                }
            });
        }

        public async Task<ServiceResult<decimal>> GetTotalExpensesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<decimal>.Failure("EXPENSE_INVALID_USER", "Utilisateur invalide.");

                    if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
                        return ServiceResult<decimal>.Failure("EXPENSE_INVALID_PERIOD", "La période demandée est invalide.");

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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetTotalExpensesAsync : {ex.Message}");
                    return ServiceResult<decimal>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors du calcul des dépenses.");
                }
            });
        }

        public async Task<ServiceResult<int>> GetExpensesCountAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null, bool? isFixedCharge = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<int>.Failure("EXPENSE_INVALID_USER", "Utilisateur invalide.");

                    if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
                        return ServiceResult<int>.Failure("EXPENSE_INVALID_PERIOD", "La période demandée est invalide.");

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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetExpensesCountAsync : {ex.Message}");
                    return ServiceResult<int>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors du calcul du nombre de dépenses.");
                }
            });
        }

        public async Task<ServiceResult<Expense>> DuplicateExpenseAsync(int expenseId, int userId, DateTime? newDate = null)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (expenseId <= 0 || userId <= 0)
                        return ServiceResult<Expense>.Failure("EXPENSE_INVALID_INPUT", "Les informations demandées sont invalides.");

                    Expense? existingExpense = _dbContext.GetExpenseById(expenseId, userId);
                    if (existingExpense == null)
                        return ServiceResult<Expense>.Failure("EXPENSE_NOT_FOUND", "Dépense introuvable.");

                    var duplicatedExpense = new Expense
                    {
                        UserId = existingExpense.UserId,
                        CategoryId = existingExpense.CategoryId,
                        Amount = existingExpense.Amount,
                        Note = existingExpense.Note,
                        IsFixedCharge = existingExpense.IsFixedCharge,
                        DateOperation = newDate ?? DateTime.Now
                    };

                    return await CreateExpenseAsync(duplicatedExpense);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur DuplicateExpenseAsync : {ex.Message}");
                    return ServiceResult<Expense>.Failure("EXPENSE_UNEXPECTED_ERROR", "Une erreur est survenue lors de la duplication de la dépense.");
                }
            });
        }

        /// <summary>
        /// Valide les données métier d'une dépense.
        /// </summary>
        private static ServiceResult ValidateExpense(Expense expense)
        {
            if (expense.UserId <= 0)
                return ServiceResult.Failure("EXPENSE_INVALID_USER", "Utilisateur invalide.");

            if (expense.CategoryId <= 0)
                return ServiceResult.Failure("EXPENSE_INVALID_CATEGORY", "Catégorie invalide.");

            if (!ValidationHelper.IsValidAmount(expense.Amount))
                return ServiceResult.Failure("EXPENSE_INVALID_AMOUNT", "Le montant doit être strictement positif.");

            if (!ValidationHelper.IsValidDate(expense.DateOperation))
                return ServiceResult.Failure("EXPENSE_INVALID_DATE", "La date de la dépense ne peut pas être dans le futur.");

            return ServiceResult.Success();
        }

        /// <summary>
        /// Applique les critères d'un filtre sur une collection de dépenses.
        /// </summary>
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
                expenses = expenses.Where(expense => expense.Note.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            return expenses;
        }

        private bool CategoryExistsForUser(int userId, int categoryId)
        {
            Category? category = _dbContext.GetCategoryById(categoryId, userId);
            return category != null && category.IsActive;
        }
    }
}
