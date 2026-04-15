using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Common;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour la gestion des catégories.
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private const string DefaultCategoryColor = "#6B7A8F";

        private readonly IMoneyMateDbContext _dbContext;

        public CategoryService()
            : this(DatabaseService.Instance)
        {
        }

        public CategoryService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public Task<ServiceResult<List<Category>>> GetCategoriesAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<Category>>.Failure(
                            "CATEGORY_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    List<Category> categories = _dbContext.GetCategoriesByUserId(userId);
                    return ServiceResult<List<Category>>.Success(categories);
                },
                operationName: nameof(GetCategoriesAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des catégories.");
        }

        public Task<ServiceResult<List<Category>>> GetInactiveCategoriesAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<Category>>.Failure(
                            "CATEGORY_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    List<Category> categories = _dbContext.GetCategoriesByUserId(userId)
                        .Where(category => !category.IsActive)
                        .OrderBy(category => category.DisplayOrder)
                        .ThenBy(category => category.Name)
                        .ToList();

                    return ServiceResult<List<Category>>.Success(categories);
                },
                operationName: nameof(GetInactiveCategoriesAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des catégories inactives.");
        }

        public Task<ServiceResult<List<Category>>> GetCustomCategoriesAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<Category>>.Failure(
                            "CATEGORY_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    List<Category> categories = _dbContext.GetCustomCategoriesByUserId(userId);
                    return ServiceResult<List<Category>>.Success(categories);
                },
                operationName: nameof(GetCustomCategoriesAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des catégories personnalisées.");
        }

        public Task<ServiceResult<Category>> GetCategoryByIdAsync(int categoryId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (categoryId <= 0 || userId <= 0)
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    Category? category = _dbContext.GetCategoryById(categoryId, userId);
                    if (category is null)
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_NOT_FOUND",
                            "Catégorie introuvable.");

                    return ServiceResult<Category>.Success(category);
                },
                operationName: nameof(GetCategoryByIdAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement de la catégorie.");
        }

        public Task<ServiceResult<Category>> CreateCategoryAsync(Category category)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(category);

                    if (category.IsSystem)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_SYSTEM_FORBIDDEN",
                            "Une catégorie système ne peut pas être créée depuis ce service.");
                    }

                    if (!category.UserId.HasValue || category.UserId.Value <= 0)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_INVALID_USER",
                            ServiceMessages.InvalidUser);
                    }

                    ServiceResult normalizationResult = NormalizeAndValidateCategory(category);
                    if (!normalizationResult.IsSuccess)
                    {
                        return ServiceResult<Category>.Failure(
                            normalizationResult.ErrorCode,
                            normalizationResult.Message);
                    }

                    ServiceResult<bool> nameExistsResult =
                        CategoryNameExistsInternal(category.UserId.Value, category.Name);

                    if (!nameExistsResult.IsSuccess)
                    {
                        return ServiceResult<Category>.Failure(
                            nameExistsResult.ErrorCode,
                            nameExistsResult.Message);
                    }

                    if (nameExistsResult.Data)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_NAME_ALREADY_EXISTS",
                            "Une catégorie portant ce nom existe déjà.");
                    }

                    category.CreatedAt = DateTime.UtcNow;
                    category.IsSystem = false;
                    category.IsActive = true;
                    category.DisplayOrder = ResolveNextDisplayOrder(category.UserId.Value);

                    int categoryId = _dbContext.InsertCategory(category);
                    if (categoryId <= 0)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_CREATE_FAILED",
                            "Impossible de créer la catégorie.");
                    }

                    category.Id = categoryId;
                    return ServiceResult<Category>.Success(category, "Catégorie créée avec succès.");
                },
                operationName: nameof(CreateCategoryAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la création de la catégorie.");
        }

        public Task<ServiceResult<Category>> UpdateCategoryAsync(Category category)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(category);

                    if (category.Id <= 0 || !category.UserId.HasValue || category.UserId.Value <= 0)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_INVALID_INPUT",
                            "Les informations de la catégorie sont invalides.");
                    }

                    Category? existingCategory = _dbContext.GetCategoryById(category.Id, category.UserId.Value);
                    if (existingCategory is null)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_NOT_FOUND",
                            "Catégorie introuvable.");
                    }

                    if (existingCategory.IsSystem)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_SYSTEM_FORBIDDEN",
                            "Une catégorie système ne peut pas être modifiée.");
                    }

                    ServiceResult normalizationResult = NormalizeAndValidateCategory(category, existingCategory);
                    if (!normalizationResult.IsSuccess)
                    {
                        return ServiceResult<Category>.Failure(
                            normalizationResult.ErrorCode,
                            normalizationResult.Message);
                    }

                    ServiceResult<bool> nameExistsResult =
                        CategoryNameExistsInternal(category.UserId.Value, category.Name, category.Id);

                    if (!nameExistsResult.IsSuccess)
                    {
                        return ServiceResult<Category>.Failure(
                            nameExistsResult.ErrorCode,
                            nameExistsResult.Message);
                    }

                    if (nameExistsResult.Data)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_NAME_ALREADY_EXISTS",
                            "Une catégorie portant ce nom existe déjà.");
                    }

                    category.IsSystem = false;
                    category.CreatedAt = existingCategory.CreatedAt;

                    int updatedRows = _dbContext.UpdateCategory(category);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_UPDATE_FAILED",
                            "La mise à jour de la catégorie a échoué.");
                    }

                    return ServiceResult<Category>.Success(category, "Catégorie mise à jour avec succès.");
                },
                operationName: nameof(UpdateCategoryAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour de la catégorie.");
        }

        public Task<ServiceResult> DeleteCategoryAsync(int categoryId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (categoryId <= 0 || userId <= 0)
                        return ServiceResult.Failure(
                            "CATEGORY_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    Category? category = _dbContext.GetCategoryById(categoryId, userId);
                    if (category is null)
                    {
                        return ServiceResult.Failure(
                            "CATEGORY_NOT_FOUND",
                            "Catégorie introuvable.");
                    }

                    if (category.IsSystem)
                    {
                        return ServiceResult.Failure(
                            "CATEGORY_SYSTEM_FORBIDDEN",
                            "Une catégorie système ne peut pas être supprimée.");
                    }

                    ServiceResult<bool> inUseResult = IsCategoryInUseInternal(categoryId, userId);
                    if (!inUseResult.IsSuccess)
                    {
                        return ServiceResult.Failure(
                            inUseResult.ErrorCode,
                            inUseResult.Message);
                    }

                    if (inUseResult.Data)
                    {
                        return ServiceResult.Failure(
                            "CATEGORY_IN_USE",
                            "Cette catégorie est utilisée par des dépenses, charges fixes ou alertes.");
                    }

                    int deletedRows = _dbContext.DeleteCategory(category);
                    if (deletedRows != 1)
                    {
                        return ServiceResult.Failure(
                            "CATEGORY_DELETE_FAILED",
                            "La suppression de la catégorie a échoué.");
                    }

                    return ServiceResult.Success("Catégorie supprimée avec succès.");
                },
                operationName: nameof(DeleteCategoryAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la suppression de la catégorie.");
        }

        public Task<ServiceResult<bool>> IsCategoryInUseAsync(int categoryId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () => IsCategoryInUseInternal(categoryId, userId),
                operationName: nameof(IsCategoryInUseAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la vérification de la catégorie.");
        }

        public Task<ServiceResult<bool>> CategoryNameExistsAsync(int userId, string categoryName, int? excludedCategoryId = null)
        {
            return ServiceExecution.ExecuteAsync(
                action: () => CategoryNameExistsInternal(userId, categoryName, excludedCategoryId),
                operationName: nameof(CategoryNameExistsAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la vérification du nom de catégorie.");
        }

        public Task<ServiceResult<Category>> SetCategoryActiveStateAsync(int categoryId, int userId, bool isActive)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (categoryId <= 0 || userId <= 0)
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    Category? category = _dbContext.GetCategoryById(categoryId, userId);
                    if (category is null)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_NOT_FOUND",
                            "Catégorie introuvable.");
                    }

                    if (category.IsSystem)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_SYSTEM_FORBIDDEN",
                            "Une catégorie système ne peut pas être modifiée.");
                    }

                    if (category.IsActive == isActive)
                        return ServiceResult<Category>.Success(category);

                    category.IsActive = isActive;

                    int updatedRows = _dbContext.UpdateCategory(category);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_UPDATE_FAILED",
                            "La mise à jour de la catégorie a échoué.");
                    }

                    return ServiceResult<Category>.Success(
                        category,
                        isActive
                            ? "Catégorie activée avec succès."
                            : "Catégorie désactivée avec succès.");
                },
                operationName: nameof(SetCategoryActiveStateAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour de la catégorie.");
        }

        public Task<ServiceResult> ReorderCategoriesAsync(int userId, IReadOnlyList<int> orderedCategoryIds)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0 || orderedCategoryIds is null)
                        return ServiceResult.Failure(
                            "CATEGORY_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    List<Category> customCategories = _dbContext.GetCustomCategoriesByUserId(userId);
                    HashSet<int> customCategoryIds = customCategories
                        .Select(category => category.Id)
                        .ToHashSet();

                    bool isInvalidOrder =
                        orderedCategoryIds.Count == 0 ||
                        !orderedCategoryIds.All(customCategoryIds.Contains) ||
                        orderedCategoryIds.Distinct().Count() != orderedCategoryIds.Count;

                    if (isInvalidOrder)
                    {
                        return ServiceResult.Failure(
                            "CATEGORY_INVALID_ORDER",
                            "L'ordre des catégories est invalide.");
                    }

                    int displayOrder = 1;

                    foreach (int categoryId in orderedCategoryIds)
                    {
                        Category category = customCategories.First(existingCategory => existingCategory.Id == categoryId);
                        category.DisplayOrder = displayOrder++;

                        if (_dbContext.UpdateCategory(category) != 1)
                        {
                            return ServiceResult.Failure(
                                "CATEGORY_REORDER_FAILED",
                                "La mise à jour de l'ordre des catégories a échoué.");
                        }
                    }

                    return ServiceResult.Success("Ordre des catégories mis à jour avec succès.");
                },
                operationName: nameof(ReorderCategoriesAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du réordonnancement des catégories.");
        }

        private ServiceResult<bool> CategoryNameExistsInternal(int userId, string categoryName, int? excludedCategoryId = null)
        {
            if (userId <= 0)
                return ServiceResult<bool>.Failure("CATEGORY_INVALID_USER", ServiceMessages.InvalidUser);

            if (string.IsNullOrWhiteSpace(categoryName))
                return ServiceResult<bool>.Failure("CATEGORY_NAME_REQUIRED", "Le nom de la catégorie est requis.");

            string normalizedCategoryName = categoryName.Trim();

            bool exists = _dbContext.GetCategoriesByUserId(userId)
                .Where(category => !excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value)
                .Any(category => string.Equals(category.Name.Trim(), normalizedCategoryName, StringComparison.OrdinalIgnoreCase));

            return ServiceResult<bool>.Success(exists);
        }

        private static ServiceResult NormalizeAndValidateCategory(Category category, Category? existingCategory = null)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return ServiceResult.Failure("CATEGORY_NAME_REQUIRED", "Le nom de la catégorie est requis.");

            category.Name = category.Name.Trim();
            category.Description = category.Description?.Trim() ?? string.Empty;
            category.Color = string.IsNullOrWhiteSpace(category.Color)
                ? existingCategory?.Color ?? DefaultCategoryColor
                : category.Color.Trim();
            category.Icon = category.Icon?.Trim() ?? string.Empty;

            return ServiceResult.Success();
        }

        private int ResolveNextDisplayOrder(int userId)
        {
            return _dbContext.GetCustomCategoriesByUserId(userId)
                .Select(category => category.DisplayOrder)
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        private ServiceResult<bool> IsCategoryInUseInternal(int categoryId, int userId)
        {
            if (categoryId <= 0 || userId <= 0)
            {
                return ServiceResult<bool>.Failure(
                    "CATEGORY_INVALID_INPUT",
                    ServiceMessages.InvalidInput);
            }

            bool isInUse =
                _dbContext.GetExpensesByCategory(userId, categoryId).Count > 0 ||
                _dbContext.GetFixedChargesByUserId(userId).Any(fixedCharge => fixedCharge.CategoryId == categoryId) ||
                _dbContext.GetAlertThresholdsByUserId(userId).Any(alert => alert.CategoryId == categoryId);

            return ServiceResult<bool>.Success(isInUse);
        }
    }
}
