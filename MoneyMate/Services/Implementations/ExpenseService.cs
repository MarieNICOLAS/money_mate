using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
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

                    IEnumerable<Expense> expenses = _dbContext.GetExpensesByUserId(userId);

                    if (categoryId.HasValue && categoryId.Value > 0)
                        expenses = expenses.Where(expense => expense.CategoryId == categoryId.Value);

                    if (startDate.HasValue)
                        expenses = expenses.Where(expense => expense.DateOperation >= startDate.Value);

                    if (endDate.HasValue)
                        expenses = expenses.Where(expense => expense.DateOperation <= endDate.Value);

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
    }
}
