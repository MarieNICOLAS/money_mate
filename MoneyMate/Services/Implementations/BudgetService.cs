using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// ImplÃ©mentation du service mÃ©tier pour la gestion des budgets mensuels globaux.
    /// </summary>
    public class BudgetService : IBudgetService
    {
        private readonly IMoneyMateDbContext _dbContext;

        public BudgetService()
            : this(DatabaseService.Instance)
        {
        }

        public BudgetService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<ServiceResult<List<Budget>>> GetBudgetsAsync(int userId)
        {
            return await Task.Run(() =>
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
            return await Task.Run(() =>
            {
                try
                {
                    if (budgetId <= 0 || userId <= 0)
                        return ServiceResult<Budget>.Failure("BUDGET_INVALID_INPUT", "Les informations demandÃ©es sont invalides.");

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget == null)
                        return ServiceResult<Budget>.Failure("BUDGET_NOT_FOUND", "Budget introuvable.");

                    budget.NormalizeToMonthlyPeriod();
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

                    budget.NormalizeToMonthlyPeriod();

                    ServiceResult validationResult = ValidateBudget(budget);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<Budget>.Failure(validationResult.ErrorCode, validationResult.Message);

                    ServiceResult<bool> conflictResult = HasActiveBudgetConflictInternal(budget);
                    if (!conflictResult.IsSuccess)
                        return ServiceResult<Budget>.Failure(conflictResult.ErrorCode, conflictResult.Message);

                    if (conflictResult.Data)
                        return ServiceResult<Budget>.Failure("BUDGET_CONFLICT", "Un budget existe dÃ©jÃ  pour ce mois.");

                    budget.CreatedAt = DateTime.UtcNow;
                    budget.IsActive = true;

                    int budgetId = _dbContext.InsertBudget(budget);
                    if (budgetId <= 0)
                        return ServiceResult<Budget>.Failure("BUDGET_CREATE_FAILED", "Impossible de crÃ©er le budget.");

                    budget.Id = budgetId;
                    return ServiceResult<Budget>.Success(budget, "Budget crÃ©Ã© avec succÃ¨s.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur CreateBudgetAsync : {ex.Message}");
                    return ServiceResult<Budget>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors de la crÃ©ation du budget.");
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
                        return ServiceResult<Budget>.Failure("BUDGET_INVALID_ID", "Le budget Ã  modifier est invalide.");

                    budget.NormalizeToMonthlyPeriod();

                    ServiceResult validationResult = ValidateBudget(budget);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<Budget>.Failure(validationResult.ErrorCode, validationResult.Message);

                    Budget? existingBudget = _dbContext.GetBudgetById(budget.Id, budget.UserId);
                    if (existingBudget == null)
                        return ServiceResult<Budget>.Failure("BUDGET_NOT_FOUND", "Budget introuvable.");

                    existingBudget.NormalizeToMonthlyPeriod();
                    bool monthChanged = existingBudget.StartDate.Year != budget.StartDate.Year
                        || existingBudget.StartDate.Month != budget.StartDate.Month;

                    if (monthChanged)
                    {
                        ServiceResult<bool> conflictResult = HasActiveBudgetConflictInternal(budget, budget.Id);
                        if (!conflictResult.IsSuccess)
                            return ServiceResult<Budget>.Failure(conflictResult.ErrorCode, conflictResult.Message);

                        if (conflictResult.Data)
                            return ServiceResult<Budget>.Failure("BUDGET_CONFLICT", "Un budget existe dÃ©jÃ  pour ce mois.");
                    }

                    int updatedRows = _dbContext.UpdateBudget(budget);
                    if (updatedRows != 1)
                        return ServiceResult<Budget>.Failure("BUDGET_UPDATE_FAILED", "La mise Ã  jour du budget a Ã©chouÃ©.");

                    return ServiceResult<Budget>.Success(budget, "Budget mis Ã  jour avec succÃ¨s.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur UpdateBudgetAsync : {ex.Message}");
                    return ServiceResult<Budget>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors de la mise Ã  jour du budget.");
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
                        return ServiceResult.Failure("BUDGET_INVALID_INPUT", "Les informations demandÃ©es sont invalides.");

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget == null)
                        return ServiceResult.Failure("BUDGET_NOT_FOUND", "Budget introuvable.");

                    int deletedRows = _dbContext.DeleteBudget(budget);
                    if (deletedRows != 1)
                        return ServiceResult.Failure("BUDGET_DELETE_FAILED", "La suppression du budget a Ã©chouÃ©.");

                    return ServiceResult.Success("Budget supprimÃ© avec succÃ¨s.");
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

                    budget.NormalizeToMonthlyPeriod();

                    DateTime endDate = budget.EndDate ?? DateTime.MaxValue;
                    decimal consumedAmount = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate)
                        .Sum(expense => expense.Amount);

                    return ServiceResult<decimal>.Success(consumedAmount);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetConsumedAmountAsync : {ex.Message}");
                    return ServiceResult<decimal>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors du calcul du montant consommÃ©.");
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

        public async Task<ServiceResult<bool>> HasActiveBudgetConflictAsync(Budget budget, int? excludedBudgetId = null)
            => await Task.Run(() => HasActiveBudgetConflictInternal(budget, excludedBudgetId));

        private static ServiceResult ValidateBudget(Budget budget)
        {
            if (budget.UserId <= 0)
                return ServiceResult.Failure("BUDGET_INVALID_USER", "Utilisateur invalide.");

            if (!ValidationHelper.IsValidAmount(budget.Amount))
                return ServiceResult.Failure("BUDGET_INVALID_AMOUNT", "Le montant du budget doit Ãªtre strictement positif.");

            if (budget.StartDate.Date > DateTime.Today)
                return ServiceResult.Failure("BUDGET_FUTURE_MONTH_NOT_ALLOWED", "Impossible de crÃ©er un budget pour un mois futur.");

            if (!string.Equals(budget.PeriodType, "Monthly", StringComparison.OrdinalIgnoreCase))
                return ServiceResult.Failure("BUDGET_INVALID_PERIOD_TYPE", "Seuls les budgets mensuels sont autorisÃ©s.");

            return ServiceResult.Success();
        }

        private ServiceResult<bool> HasActiveBudgetConflictInternal(Budget budget, int? excludedBudgetId = null)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(budget);

                if (budget.UserId <= 0)
                    return ServiceResult<bool>.Failure("BUDGET_INVALID_INPUT", "Les informations du budget sont invalides.");

                DateTime targetMonth = new DateTime(budget.StartDate.Year, budget.StartDate.Month, 1);

                bool hasConflict = _dbContext.GetBudgetsByUserId(budget.UserId)
                    .Where(existingBudget => !excludedBudgetId.HasValue || existingBudget.Id != excludedBudgetId.Value)
                    .Any(existingBudget => existingBudget.StartDate.Year == targetMonth.Year &&
                                           existingBudget.StartDate.Month == targetMonth.Month);

                return ServiceResult<bool>.Success(hasConflict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur HasActiveBudgetConflictInternal : {ex.Message}");
                return ServiceResult<bool>.Failure("BUDGET_UNEXPECTED_ERROR", "Une erreur est survenue lors de la vÃ©rification des conflits de budget.");
            }
        }
    }
}
