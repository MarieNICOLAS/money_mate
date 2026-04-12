using MoneyMate.Models;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service métier pour la gestion des budgets mensuels globaux.
    /// </summary>
    public interface IBudgetService
    {
        Task<ServiceResult<List<Budget>>> GetBudgetsAsync(int userId);

        Task<ServiceResult<Budget>> GetBudgetByIdAsync(int budgetId, int userId);

        Task<ServiceResult<Budget>> CreateBudgetAsync(Budget budget);

        Task<ServiceResult<Budget>> UpdateBudgetAsync(Budget budget);

        Task<ServiceResult> DeleteBudgetAsync(int budgetId, int userId);

        Task<ServiceResult<decimal>> GetConsumedAmountAsync(int budgetId, int userId);

        Task<ServiceResult<decimal>> GetConsumedPercentageAsync(int budgetId, int userId);

        Task<ServiceResult<BudgetConsumptionSummary>> GetBudgetConsumptionSummaryAsync(int budgetId, int userId);

        Task<ServiceResult<bool>> HasActiveBudgetConflictAsync(Budget budget, int? excludedBudgetId = null);
    }
}
