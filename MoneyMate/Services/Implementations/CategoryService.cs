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
        private readonly IMoneyMateDbContext _dbContext;

        public CategoryService()
            : this(DatabaseService.Instance)
        {
        }

        public CategoryService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<ServiceResult<List<Category>>> GetCategoriesAsync(int userId)
        {
            return await Task.Run(async () =>
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

        public async Task<ServiceResult<List<Category>>> GetInactiveCategoriesAsync(int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<Category>>.Failure("CATEGORY_INVALID_USER", "Utilisateur invalide.");

                    List<Category> categories = _dbContext.GetCategoriesByUserId(userId)
                        .Where(category => !category.IsActive)
                        .OrderBy(category => category.DisplayOrder)
                        .ThenBy(category => category.Name)
                        .ToList();

                    return ServiceResult<List<Category>>.Success(categories);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetInactiveCategoriesAsync : {ex.Message}");
                    return ServiceResult<List<Category>>.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des catégories inactives.");
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

                    ServiceResult normalizationResult = NormalizeAndValidateCategory(category);
                    if (!normalizationResult.IsSuccess)
                        return ServiceResult<Category>.Failure(normalizationResult.ErrorCode, normalizationResult.Message);

                    ServiceResult<bool> nameExistsResult = CategoryNameExistsInternal(category.UserId.Value, category.Name);
                    if (!nameExistsResult.IsSuccess)
                        return ServiceResult<Category>.Failure(nameExistsResult.ErrorCode, nameExistsResult.Message);

                    if (nameExistsResult.Data)
                        return ServiceResult<Category>.Failure("CATEGORY_NAME_ALREADY_EXISTS", "Une catégorie portant ce nom existe déjà.");

                    category.CreatedAt = DateTime.UtcNow;
                    category.IsSystem = false;
                    category.IsActive = true;
                    category.DisplayOrder = ResolveNextDisplayOrder(category.UserId.Value);

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

                    Category? existingCategory = _dbContext.GetCategoryById(category.Id, category.UserId.Value);
                    if (existingCategory == null)
                        return ServiceResult<Category>.Failure("CATEGORY_NOT_FOUND", "Catégorie introuvable.");

                    if (existingCategory.IsSystem)
                        return ServiceResult<Category>.Failure("CATEGORY_SYSTEM_FORBIDDEN", "Une catégorie système ne peut pas être modifiée.");

                    ServiceResult normalizationResult = NormalizeAndValidateCategory(category, existingCategory);
                    if (!normalizationResult.IsSuccess)
                        return ServiceResult<Category>.Failure(normalizationResult.ErrorCode, normalizationResult.Message);

                    ServiceResult<bool> nameExistsResult = CategoryNameExistsInternal(category.UserId.Value, category.Name, category.Id);
                    if (!nameExistsResult.IsSuccess)
                        return ServiceResult<Category>.Failure(nameExistsResult.ErrorCode, nameExistsResult.Message);

                    if (nameExistsResult.Data)
                        return ServiceResult<Category>.Failure("CATEGORY_NAME_ALREADY_EXISTS", "Une catégorie portant ce nom existe déjà.");

                    category.IsSystem = false;
                    category.CreatedAt = existingCategory.CreatedAt;

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

                    ServiceResult<bool> inUseResult = IsCategoryInUseInternal(categoryId, userId);
                    if (!inUseResult.IsSuccess)
                        return ServiceResult.Failure(inUseResult.ErrorCode, inUseResult.Message);

                    if (inUseResult.Data)
                        return ServiceResult.Failure("CATEGORY_IN_USE", "Cette catégorie est utilisée par des dépenses, budgets, charges fixes ou alertes.");

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

        public async Task<ServiceResult<bool>> IsCategoryInUseAsync(int categoryId, int userId)
            => await Task.Run(() => IsCategoryInUseInternal(categoryId, userId));

        public async Task<ServiceResult<bool>> CategoryNameExistsAsync(int userId, string categoryName, int? excludedCategoryId = null)
            => await Task.Run(() => CategoryNameExistsInternal(userId, categoryName, excludedCategoryId));

        public async Task<ServiceResult<Category>> SetCategoryActiveStateAsync(int categoryId, int userId, bool isActive)
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

                    if (category.IsSystem)
                        return ServiceResult<Category>.Failure("CATEGORY_SYSTEM_FORBIDDEN", "Une catégorie système ne peut pas être modifiée.");

                    if (category.IsActive == isActive)
                        return ServiceResult<Category>.Success(category);

                    category.IsActive = isActive;

                    int updatedRows = _dbContext.UpdateCategory(category);
                    if (updatedRows != 1)
                        return ServiceResult<Category>.Failure("CATEGORY_UPDATE_FAILED", "La mise à jour de la catégorie a échoué.");

                    return ServiceResult<Category>.Success(category, isActive
                        ? "Catégorie activée avec succès."
                        : "Catégorie désactivée avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur SetCategoryActiveStateAsync : {ex.Message}");
                    return ServiceResult<Category>.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors de la mise à jour de la catégorie.");
                }
            });
        }

        public async Task<ServiceResult> ReorderCategoriesAsync(int userId, IReadOnlyList<int> orderedCategoryIds)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0 || orderedCategoryIds == null)
                        return ServiceResult.Failure("CATEGORY_INVALID_INPUT", "Les informations demandées sont invalides.");

                    List<Category> customCategories = _dbContext.GetCustomCategoriesByUserId(userId);
                    HashSet<int> customCategoryIds = customCategories.Select(category => category.Id).ToHashSet();

                    if (orderedCategoryIds.Count == 0 || !orderedCategoryIds.All(customCategoryIds.Contains) || orderedCategoryIds.Distinct().Count() != orderedCategoryIds.Count)
                        return ServiceResult.Failure("CATEGORY_INVALID_ORDER", "L'ordre des catégories est invalide.");

                    int displayOrder = 1;
                    foreach (int categoryId in orderedCategoryIds)
                    {
                        Category category = customCategories.First(existingCategory => existingCategory.Id == categoryId);
                        category.DisplayOrder = displayOrder++;

                        if (_dbContext.UpdateCategory(category) != 1)
                            return ServiceResult.Failure("CATEGORY_REORDER_FAILED", "La mise à jour de l'ordre des catégories a échoué.");
                    }

                    return ServiceResult.Success("Ordre des catégories mis à jour avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur ReorderCategoriesAsync : {ex.Message}");
                    return ServiceResult.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors du réordonnancement des catégories.");
                }
            });
        }

        /// <summary>
        /// Vérifie si un nom de catégorie existe déjà pour un utilisateur.
        /// </summary>
        private ServiceResult<bool> CategoryNameExistsInternal(int userId, string categoryName, int? excludedCategoryId = null)
        {
            if (userId <= 0)
                return ServiceResult<bool>.Failure("CATEGORY_INVALID_USER", "Utilisateur invalide.");

            if (string.IsNullOrWhiteSpace(categoryName))
                return ServiceResult<bool>.Failure("CATEGORY_NAME_REQUIRED", "Le nom de la catégorie est requis.");

            string normalizedCategoryName = categoryName.Trim();

            bool exists = _dbContext.GetCategoriesByUserId(userId)
                .Where(category => !excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value)
                .Any(category => string.Equals(category.Name.Trim(), normalizedCategoryName, StringComparison.OrdinalIgnoreCase));

            return ServiceResult<bool>.Success(exists);
        }

        /// <summary>
        /// Normalise et valide une catégorie personnalisée.
        /// </summary>
        private static ServiceResult NormalizeAndValidateCategory(Category category, Category? existingCategory = null)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return ServiceResult.Failure("CATEGORY_NAME_REQUIRED", "Le nom de la catégorie est requis.");

            category.Name = category.Name.Trim();
            category.Description = category.Description?.Trim() ?? string.Empty;
            category.Color = string.IsNullOrWhiteSpace(category.Color)
                ? existingCategory?.Color ?? "#6B7A8F"
                : category.Color.Trim();
            category.Icon = category.Icon?.Trim() ?? string.Empty;

            return ServiceResult.Success();
        }

        /// <summary>
        /// Retourne le prochain ordre d'affichage disponible.
        /// </summary>
        private int ResolveNextDisplayOrder(int userId)
            => _dbContext.GetCustomCategoriesByUserId(userId)
                   .Select(category => category.DisplayOrder)
                   .DefaultIfEmpty(0)
                   .Max() + 1;

        /// <summary>
        /// Vérifie si une catégorie est déjà utilisée par des données métier.
        /// </summary>
        private ServiceResult<bool> IsCategoryInUseInternal(int categoryId, int userId)
        {
            try
            {
                if (categoryId <= 0 || userId <= 0)
                    return ServiceResult<bool>.Failure("CATEGORY_INVALID_INPUT", "Les informations demandées sont invalides.");

                bool isInUse = _dbContext.GetExpensesByCategory(userId, categoryId).Count > 0
                    || _dbContext.GetBudgetsByUserId(userId).Any(budget => budget.CategoryId == categoryId)
                    || _dbContext.GetFixedChargesByUserId(userId).Any(fixedCharge => fixedCharge.CategoryId == categoryId)
                    || _dbContext.GetAlertThresholdsByUserId(userId).Any(alert => alert.CategoryId == categoryId);

                return ServiceResult<bool>.Success(isInUse);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur IsCategoryInUseInternal : {ex.Message}");
                return ServiceResult<bool>.Failure("CATEGORY_UNEXPECTED_ERROR", "Une erreur est survenue lors de la vérification de la catégorie.");
            }
        }
    }
}
