using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour la gestion des budgets.
    /// </summary>
    public class BudgetService : IBudgetService
    {
        private static readonly HashSet<string> AllowedPeriodTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Weekly",
            "Monthly",
            "Yearly"
        };

        private readonly MoneyMateDbContext _dbContext;

        public BudgetService()
        {
            _dbContext = DatabaseService.Instance;
        }

        public async Task<ServiceResult<List<Budget>>> GetBudgetsAsync(int userId)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<Budget>>.Failure("BUDGET_INVALID_USER", "Utilisateur invalide.");

                    List<Budget> budgets = _dbContext.GetBudgetsByUserId(userId);
                    return ServiceResult<List<Budget>>.Success(budgets);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetBudgetsAsync : {ex.Message}");
                    return ServiceResult<List<Budget>>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des budgets.");
                }
            });
        }

        public async Task<ServiceResult<Budget>> GetBudgetByIdAsync(int budgetId, int userId)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    if (budgetId <= 0 || userId <= 0)
                        return ServiceResult<Budget>.Failure("BUDGET_INVALID_INPUT", "Les informations demandées sont invalides.");

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget == null)
                        return ServiceResult<Budget>.Failure("BUDGET_NOT_FOUND", "Budget introuvable.");

                    return ServiceResult<Budget>.Success(budget);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetBudgetByIdAsync : {ex.Message}");
                    return ServiceResult<Budget>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement du budget.");
                }
            });
        }

        public async Task<ServiceResult<Budget>> CreateBudgetAsync(Budget budget)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(budget);

                    ServiceResult validationResult = ValidateBudget(budget);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<Budget>.Failure(validationResult.ErrorCode, validationResult.Message);

                    ServiceResult<bool> conflictResult = HasActiveBudgetConflictInternal(budget);
                    if (!conflictResult.IsSuccess)
                        return ServiceResult<Budget>.Failure(conflictResult.ErrorCode, conflictResult.Message);

                    if (conflictResult.Data)
                        return ServiceResult<Budget>.Failure("BUDGET_CONFLICT", "Un budget actif existe déjà pour cette catégorie sur une période qui se chevauche.");

                    budget.PeriodType = budget.PeriodType.Trim();
                    budget.CreatedAt = DateTime.UtcNow;
                    budget.IsActive = true;

                    int budgetId = _dbContext.InsertBudget(budget);
                    if (budgetId <= 0)
                        return ServiceResult<Budget>.Failure("BUDGET_CREATE_FAILED", "Impossible de créer le budget.");

                    budget.Id = budgetId;
                    return ServiceResult<Budget>.Success(budget, "Budget créé avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur CreateBudgetAsync : {ex.Message}");
                    return ServiceResult<Budget>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors de la création du budget.");
                }
            });
        }

        public async Task<ServiceResult<Budget>> UpdateBudgetAsync(Budget budget)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(budget);

                    if (budget.Id <= 0)
                        return ServiceResult<Budget>.Failure("BUDGET_INVALID_ID", "Le budget à modifier est invalide.");

                    ServiceResult validationResult = ValidateBudget(budget);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<Budget>.Failure(validationResult.ErrorCode, validationResult.Message);

                    ServiceResult<bool> conflictResult = HasActiveBudgetConflictInternal(budget, budget.Id);
                    if (!conflictResult.IsSuccess)
                        return ServiceResult<Budget>.Failure(conflictResult.ErrorCode, conflictResult.Message);

                    if (conflictResult.Data)
                        return ServiceResult<Budget>.Failure("BUDGET_CONFLICT", "Un budget actif existe déjà pour cette catégorie sur une période qui se chevauche.");

                    int updatedRows = _dbContext.UpdateBudget(budget);
                    if (updatedRows != 1)
                        return ServiceResult<Budget>.Failure("BUDGET_UPDATE_FAILED", "La mise à jour du budget a échoué.");

                    return ServiceResult<Budget>.Success(budget, "Budget mis à jour avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur UpdateBudgetAsync : {ex.Message}");
                    return ServiceResult<Budget>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors de la mise à jour du budget.");
                }
            });
        }

        public async Task<ServiceResult> DeleteBudgetAsync(int budgetId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (budgetId <= 0 || userId <= 0)
                        return ServiceResult.Failure("BUDGET_INVALID_INPUT", "Les informations demandées sont invalides.");

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget == null)
                        return ServiceResult.Failure("BUDGET_NOT_FOUND", "Budget introuvable.");

                    int deletedRows = _dbContext.DeleteBudget(budget);
                    if (deletedRows != 1)
                        return ServiceResult.Failure("BUDGET_DELETE_FAILED", "La suppression du budget a échoué.");

                    return ServiceResult.Success("Budget supprimé avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur DeleteBudgetAsync : {ex.Message}");
                    return ServiceResult.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors de la suppression du budget.");
                }
            });
        }

        public async Task<ServiceResult<decimal>> GetConsumedAmountAsync(int budgetId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget == null)
                        return ServiceResult<decimal>.Failure("BUDGET_NOT_FOUND", "Budget introuvable.");

                    DateTime endDate = budget.EndDate ?? DateTime.MaxValue;
                    decimal consumedAmount = _dbContext.GetExpensesByCategory(userId, budget.CategoryId)
                        .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate)
                        .Sum(expense => expense.Amount);

                    return ServiceResult<decimal>.Success(consumedAmount);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetConsumedAmountAsync : {ex.Message}");
                    return ServiceResult<decimal>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors du calcul du montant consommé.");
                }
            });
        }

        public async Task<ServiceResult<decimal>> GetConsumedPercentageAsync(int budgetId, int userId)
        {
            ServiceResult<decimal> consumedAmountResult = await GetConsumedAmountAsync(budgetId, userId);
            if (!consumedAmountResult.IsSuccess)
                return ServiceResult<decimal>.Failure(consumedAmountResult.ErrorCode, consumedAmountResult.Message);

            Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
            if (budget == null)
                return ServiceResult<decimal>.Failure("BUDGET_NOT_FOUND", "Budget introuvable.");

            decimal percentage = budget.CalculateBudgetPercentage(consumedAmountResult.Data);
            return ServiceResult<decimal>.Success(percentage);
        }

        public async Task<ServiceResult<BudgetConsumptionSummary>> GetBudgetConsumptionSummaryAsync(int budgetId, int userId)
        {
            ServiceResult<decimal> consumedAmountResult = await GetConsumedAmountAsync(budgetId, userId);
            if (!consumedAmountResult.IsSuccess)
                return ServiceResult<BudgetConsumptionSummary>.Failure(consumedAmountResult.ErrorCode, consumedAmountResult.Message);

            Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
            if (budget == null)
                return ServiceResult<BudgetConsumptionSummary>.Failure("BUDGET_NOT_FOUND", "Budget introuvable.");

            decimal consumedAmount = consumedAmountResult.Data;
            decimal consumedPercentage = budget.CalculateBudgetPercentage(consumedAmount);
            decimal remainingAmount = budget.Amount - consumedAmount;

            BudgetConsumptionSummary summary = new()
            {
                Budget = budget,
                ConsumedAmount = consumedAmount,
                ConsumedPercentage = consumedPercentage,
                RemainingAmount = remainingAmount,
                IsExceeded = consumedAmount > budget.Amount
            };

            return ServiceResult<BudgetConsumptionSummary>.Success(summary);
        }

        public async Task<ServiceResult<List<Budget>>> GetBudgetsByCategoryAsync(int userId, int categoryId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0 || categoryId <= 0)
                        return ServiceResult<List<Budget>>.Failure("BUDGET_INVALID_INPUT", "Les informations demandées sont invalides.");

                    List<Budget> budgets = _dbContext.GetBudgetsByUserId(userId)
                        .Where(budget => budget.CategoryId == categoryId)
                        .OrderByDescending(budget => budget.StartDate)
                        .ThenByDescending(budget => budget.CreatedAt)
                        .ToList();

                    return ServiceResult<List<Budget>>.Success(budgets);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetBudgetsByCategoryAsync : {ex.Message}");
                    return ServiceResult<List<Budget>>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des budgets.");
                }
            });
        }

        public async Task<ServiceResult<bool>> HasActiveBudgetConflictAsync(Budget budget, int? excludedBudgetId = null)
            => await Task.Run(() => HasActiveBudgetConflictInternal(budget, excludedBudgetId));

        /// <summary>
        /// Valide les données métier d'un budget.
        /// </summary>
        private static ServiceResult ValidateBudget(Budget budget)
        {
            if (budget.UserId <= 0)
                return ServiceResult.Failure("BUDGET_INVALID_USER", "Utilisateur invalide.");

            if (budget.CategoryId <= 0)
                return ServiceResult.Failure("BUDGET_INVALID_CATEGORY", "Catégorie invalide.");

            if (!ValidationHelper.IsValidAmount(budget.Amount))
                return ServiceResult.Failure("BUDGET_INVALID_AMOUNT", "Le montant du budget doit être strictement positif.");

            if (string.IsNullOrWhiteSpace(budget.PeriodType) || !AllowedPeriodTypes.Contains(budget.PeriodType.Trim()))
                return ServiceResult.Failure("BUDGET_INVALID_PERIOD_TYPE", "Le type de période est invalide.");

            if (budget.EndDate.HasValue && budget.StartDate > budget.EndDate.Value)
                return ServiceResult.Failure("BUDGET_INVALID_PERIOD", "La période du budget est invalide.");

            return ServiceResult.Success();
        }

        /// <summary>
        /// Indique si deux périodes se chevauchent.
        /// </summary>
        private static bool PeriodsOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
            => start1 <= end2 && start2 <= end1;

        /// <summary>
        /// Vérifie si un budget actif entre en conflit avec un autre budget existant.
        /// </summary>
        private ServiceResult<bool> HasActiveBudgetConflictInternal(Budget budget, int? excludedBudgetId = null)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(budget);

                if (budget.UserId <= 0 || budget.CategoryId <= 0)
                    return ServiceResult<bool>.Failure("BUDGET_INVALID_INPUT", "Les informations du budget sont invalides.");

                DateTime startDate = budget.StartDate;
                DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

                bool hasConflict = _dbContext.GetBudgetsByUserId(budget.UserId)
                    .Where(existingBudget => existingBudget.CategoryId == budget.CategoryId)
                    .Where(existingBudget => !excludedBudgetId.HasValue || existingBudget.Id != excludedBudgetId.Value)
                    .Any(existingBudget => PeriodsOverlap(
                        startDate,
                        endDate,
                        existingBudget.StartDate,
                        existingBudget.EndDate ?? DateTime.MaxValue));

                return ServiceResult<bool>.Success(hasConflict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur HasActiveBudgetConflictInternal : {ex.Message}");
                return ServiceResult<bool>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors de la vérification des conflits de budget.");
            }
        }
    }
}
