using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour la gestion des catégories.
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly MoneyMateDbContext _dbContext;

        public CategoryService()
        {
            _dbContext = DatabaseService.Instance;
        }

        public async Task<ServiceResult<List<Category>>> GetCategoriesAsync(int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<Category>>.Failure("CATEGORY_INVALID_USER", "Utilisateur invalide.");

                    List<Category> categories = _dbContext.GetCategoriesByUserId(userId);
                    return ServiceResult<List<Category>>.Success(categories);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetCategoriesAsync : {ex.Message}");
                    return ServiceResult<List<Category>>.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des catégories.");
                }
            });
        }

        public async Task<ServiceResult<List<Category>>> GetCustomCategoriesAsync(int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<Category>>.Failure("CATEGORY_INVALID_USER", "Utilisateur invalide.");

                    List<Category> categories = _dbContext.GetCustomCategoriesByUserId(userId);
                    return ServiceResult<List<Category>>.Success(categories);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetCustomCategoriesAsync : {ex.Message}");
                    return ServiceResult<List<Category>>.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des catégories personnalisées.");
                }
            });
        }

        public async Task<ServiceResult<Category>> GetCategoryByIdAsync(int categoryId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (categoryId <= 0 || userId <= 0)
                        return ServiceResult<Category>.Failure("CATEGORY_INVALID_INPUT", "Les informations demandées sont invalides.");

                    Category? category = _dbContext.GetCategoryById(categoryId, userId);
                    if (category == null)
                        return ServiceResult<Category>.Failure("CATEGORY_NOT_FOUND", "Catégorie introuvable.");

                    return ServiceResult<Category>.Success(category);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetCategoryByIdAsync : {ex.Message}");
                    return ServiceResult<Category>.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement de la catégorie.");
                }
            });
        }

        public async Task<ServiceResult<Category>> CreateCategoryAsync(Category category)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(category);

                    if (category.IsSystem)
                        return ServiceResult<Category>.Failure("CATEGORY_SYSTEM_FORBIDDEN", "Une catégorie système ne peut pas être créée depuis ce service.");

                    if (!category.UserId.HasValue || category.UserId.Value <= 0)
                        return ServiceResult<Category>.Failure("CATEGORY_INVALID_USER", "Utilisateur invalide.");

                    if (string.IsNullOrWhiteSpace(category.Name))
                        return ServiceResult<Category>.Failure("CATEGORY_NAME_REQUIRED", "Le nom de la catégorie est requis.");

                    category.Name = category.Name.Trim();
                    category.Description = category.Description?.Trim() ?? string.Empty;
                    category.Color = string.IsNullOrWhiteSpace(category.Color) ? "#6B7A8F" : category.Color.Trim();
                    category.Icon = category.Icon?.Trim() ?? string.Empty;
                    category.CreatedAt = DateTime.UtcNow;
                    category.IsSystem = false;
                    category.IsActive = true;

                    int categoryId = _dbContext.InsertCategory(category);
                    if (categoryId <= 0)
                        return ServiceResult<Category>.Failure("CATEGORY_CREATE_FAILED", "Impossible de créer la catégorie.");

                    category.Id = categoryId;
                    return ServiceResult<Category>.Success(category, "Catégorie créée avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur CreateCategoryAsync : {ex.Message}");
                    return ServiceResult<Category>.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors de la création de la catégorie.");
                }
            });
        }

        public async Task<ServiceResult<Category>> UpdateCategoryAsync(Category category)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(category);

                    if (category.Id <= 0 || !category.UserId.HasValue || category.UserId.Value <= 0)
                        return ServiceResult<Category>.Failure("CATEGORY_INVALID_INPUT", "Les informations de la catégorie sont invalides.");

                    if (string.IsNullOrWhiteSpace(category.Name))
                        return ServiceResult<Category>.Failure("CATEGORY_NAME_REQUIRED", "Le nom de la catégorie est requis.");

                    Category? existingCategory = _dbContext.GetCategoryById(category.Id, category.UserId.Value);
                    if (existingCategory == null)
                        return ServiceResult<Category>.Failure("CATEGORY_NOT_FOUND", "Catégorie introuvable.");

                    if (existingCategory.IsSystem)
                        return ServiceResult<Category>.Failure("CATEGORY_SYSTEM_FORBIDDEN", "Une catégorie système ne peut pas être modifiée.");

                    category.Name = category.Name.Trim();
                    category.Description = category.Description?.Trim() ?? string.Empty;
                    category.Color = string.IsNullOrWhiteSpace(category.Color) ? existingCategory.Color : category.Color.Trim();
                    category.Icon = category.Icon?.Trim() ?? string.Empty;
                    category.IsSystem = false;

                    int updatedRows = _dbContext.UpdateCategory(category);
                    if (updatedRows != 1)
                        return ServiceResult<Category>.Failure("CATEGORY_UPDATE_FAILED", "La mise à jour de la catégorie a échoué.");

                    return ServiceResult<Category>.Success(category, "Catégorie mise à jour avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur UpdateCategoryAsync : {ex.Message}");
                    return ServiceResult<Category>.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors de la mise à jour de la catégorie.");
                }
            });
        }

        public async Task<ServiceResult> DeleteCategoryAsync(int categoryId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (categoryId <= 0 || userId <= 0)
                        return ServiceResult.Failure("CATEGORY_INVALID_INPUT", "Les informations demandées sont invalides.");

                    Category? category = _dbContext.GetCategoryById(categoryId, userId);
                    if (category == null)
                        return ServiceResult.Failure("CATEGORY_NOT_FOUND", "Catégorie introuvable.");

                    if (category.IsSystem)
                        return ServiceResult.Failure("CATEGORY_SYSTEM_FORBIDDEN", "Une catégorie système ne peut pas être supprimée.");

                    int deletedRows = _dbContext.DeleteCategory(category);
                    if (deletedRows != 1)
                        return ServiceResult.Failure("CATEGORY_DELETE_FAILED", "La suppression de la catégorie a échoué.");

                    return ServiceResult.Success("Catégorie supprimée avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur DeleteCategoryAsync : {ex.Message}");
                    return ServiceResult.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors de la suppression de la catégorie.");
                }
            });
        }
    }
}
