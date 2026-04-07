using MoneyMate.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service métier pour la gestion des catégories.
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Retourne les catégories accessibles à un utilisateur.
        /// </summary>
        Task<ServiceResult<List<Category>>> GetCategoriesAsync(int userId);

        /// <summary>
        /// Retourne les catégories personnalisées d'un utilisateur.
        /// </summary>
        Task<ServiceResult<List<Category>>> GetCustomCategoriesAsync(int userId);

        /// <summary>
        /// Retourne une catégorie accessible à un utilisateur.
        /// </summary>
        Task<ServiceResult<Category>> GetCategoryByIdAsync(int categoryId, int userId);

        /// <summary>
        /// Crée une nouvelle catégorie.
        /// </summary>
        Task<ServiceResult<Category>> CreateCategoryAsync(Category category);

        /// <summary>
        /// Met à jour une catégorie personnalisée.
        /// </summary>
        Task<ServiceResult<Category>> UpdateCategoryAsync(Category category);

        /// <summary>
        /// Supprime une catégorie personnalisée.
        /// </summary>
        Task<ServiceResult> DeleteCategoryAsync(int categoryId, int userId);
    }
}
