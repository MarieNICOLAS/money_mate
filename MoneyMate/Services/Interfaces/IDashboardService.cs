using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service métier pour l'alimentation du tableau de bord.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Retourne le résumé principal du tableau de bord.
        /// </summary>
        Task<ServiceResult<DashboardSummary>> GetDashboardSummaryAsync(int userId);

        /// <summary>
        /// Retourne les catégories les plus dépensières du mois courant.
        /// </summary>
        Task<ServiceResult<List<DashboardCategorySpending>>> GetTopSpendingCategoriesAsync(int userId, int topCount = 5);
    }
}
