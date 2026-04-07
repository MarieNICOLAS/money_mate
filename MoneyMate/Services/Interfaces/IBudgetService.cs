using MoneyMate.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service métier pour la gestion des budgets.
    /// </summary>
    public interface IBudgetService
    {
        /// <summary>
        /// Retourne les budgets actifs d'un utilisateur.
        /// </summary>
        Task<ServiceResult<List<Budget>>> GetBudgetsAsync(int userId);

        /// <summary>
        /// Retourne un budget appartenant à un utilisateur.
        /// </summary>
        Task<ServiceResult<Budget>> GetBudgetByIdAsync(int budgetId, int userId);

        /// <summary>
        /// Crée un nouveau budget.
        /// </summary>
        Task<ServiceResult<Budget>> CreateBudgetAsync(Budget budget);

        /// <summary>
        /// Met à jour un budget.
        /// </summary>
        Task<ServiceResult<Budget>> UpdateBudgetAsync(Budget budget);

        /// <summary>
        /// Supprime un budget.
        /// </summary>
        Task<ServiceResult> DeleteBudgetAsync(int budgetId, int userId);

        /// <summary>
        /// Calcule le montant consommé pour un budget.
        /// </summary>
        Task<ServiceResult<decimal>> GetConsumedAmountAsync(int budgetId, int userId);

        /// <summary>
        /// Calcule le pourcentage consommé pour un budget.
        /// </summary>
        Task<ServiceResult<decimal>> GetConsumedPercentageAsync(int budgetId, int userId);
    }
}
