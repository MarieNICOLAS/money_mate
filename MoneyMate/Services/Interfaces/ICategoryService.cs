using MoneyMate.Models;
using MoneyMate.Services.Models;
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
        /// Retourne les catégories préparées pour l'affichage avec jauges de seuil.
        /// </summary>
        Task<ServiceResult<List<CategoryListItemDto>>> GetCategoryListItemsAsync(int userId);

        /// <summary>
        /// Retourne les catégories personnalisées d'un utilisateur.
        /// </summary>
        Task<ServiceResult<List<Category>>> GetCustomCategoriesAsync(int userId);

        /// <summary>
        /// Retourne les catégories inactives d'un utilisateur.
        /// </summary>
        Task<ServiceResult<List<Category>>> GetInactiveCategoriesAsync(int userId);

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
        /// Crée ou met à jour l'override utilisateur d'une catégorie système.
        /// </summary>
        Task<ServiceResult<Category>> CustomizeSystemCategoryAsync(Category category);

        /// <summary>
        /// Supprime une catégorie personnalisée.
        /// </summary>
        Task<ServiceResult> DeleteCategoryAsync(int categoryId, int userId);

        /// <summary>
        /// Indique si une catégorie est utilisée par des données métier.
        /// </summary>
        Task<ServiceResult<bool>> IsCategoryInUseAsync(int categoryId, int userId);

        /// <summary>
        /// Indique si un nom de catégorie existe déjà pour l'utilisateur.
        /// </summary>
        Task<ServiceResult<bool>> CategoryNameExistsAsync(int userId, string categoryName, int? excludedCategoryId = null);

        /// <summary>
        /// Active ou désactive une catégorie personnalisée.
        /// </summary>
        Task<ServiceResult<Category>> SetCategoryActiveStateAsync(int categoryId, int userId, bool isActive);

        /// <summary>
        /// Met à jour l'ordre d'affichage des catégories personnalisées.
        /// </summary>
        Task<ServiceResult> ReorderCategoriesAsync(int userId, IReadOnlyList<int> orderedCategoryIds);
    }
}
