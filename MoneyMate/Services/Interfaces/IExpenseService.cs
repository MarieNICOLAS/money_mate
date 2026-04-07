using MoneyMate.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service métier pour la gestion des dépenses.
    /// </summary>
    public interface IExpenseService
    {
        /// <summary>
        /// Retourne toutes les dépenses d'un utilisateur.
        /// </summary>
        Task<ServiceResult<List<Expense>>> GetExpensesAsync(int userId);

        /// <summary>
        /// Retourne les dépenses d'un utilisateur pour une catégorie.
        /// </summary>
        Task<ServiceResult<List<Expense>>> GetExpensesByCategoryAsync(int userId, int categoryId);

        /// <summary>
        /// Retourne les dépenses d'un utilisateur pour une période.
        /// </summary>
        Task<ServiceResult<List<Expense>>> GetExpensesByPeriodAsync(int userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retourne une dépense appartenant à un utilisateur.
        /// </summary>
        Task<ServiceResult<Expense>> GetExpenseByIdAsync(int expenseId, int userId);

        /// <summary>
        /// Crée une nouvelle dépense.
        /// </summary>
        Task<ServiceResult<Expense>> CreateExpenseAsync(Expense expense);

        /// <summary>
        /// Met à jour une dépense.
        /// </summary>
        Task<ServiceResult<Expense>> UpdateExpenseAsync(Expense expense);

        /// <summary>
        /// Supprime une dépense.
        /// </summary>
        Task<ServiceResult> DeleteExpenseAsync(int expenseId, int userId);

        /// <summary>
        /// Calcule le total des dépenses d'un utilisateur.
        /// </summary>
        Task<ServiceResult<decimal>> GetTotalExpensesAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null);
    }
}
