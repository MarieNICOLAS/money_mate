using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Common;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
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
        private readonly Lock _cacheLock = new();
        private readonly Dictionary<int, List<Category>> _visibleCategoryCache = [];

        public CategoryService()
            : this(DbContextFactory.CreateDefault())
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

                    List<Category> categories = GetCachedVisibleCategories(userId, includeInactive: false);
                    return ServiceResult<List<Category>>.Success(categories);
                },
                operationName: nameof(GetCategoriesAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des catégories.");
        }

        public Task<ServiceResult<List<CategoryListItemDto>>> GetCategoryListItemsAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                    {
                        return ServiceResult<List<CategoryListItemDto>>.Failure(
                            "CATEGORY_INVALID_USER",
                            ServiceMessages.InvalidUser);
                    }

                    List<Category> categories = GetCachedVisibleCategories(userId, includeInactive: true);
                    List<AlertThreshold> alertThresholds = _dbContext.GetAlertThresholdsByUserId(userId);
                    List<Budget> budgets = _dbContext.GetBudgetsByUserId(userId);
                    List<Expense> expenses = _dbContext.GetExpensesByUserId(userId);

                    List<CategoryListItemDto> items = BuildCategoryListItems(categories, alertThresholds, budgets, expenses);
                    return ServiceResult<List<CategoryListItemDto>>.Success(items);
                },
                operationName: nameof(GetCategoryListItemsAsync),
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

                    List<Category> categories = _dbContext.GetAllCategoriesByUserId(userId)
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
                    category.IsActive = category.IsActive;
                    category.DisplayOrder = ResolveNextDisplayOrder(category.UserId.Value);

                    int categoryId = _dbContext.InsertCategory(category);
                    if (categoryId <= 0)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_CREATE_FAILED",
                            "Impossible de créer la catégorie.");
                    }

                    category.Id = categoryId;
                    InvalidateCategoryCache(category.UserId.Value);
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
                    category.ParentCategoryId = existingCategory.ParentCategoryId;
                    category.DisplayOrder = existingCategory.DisplayOrder;

                    int updatedRows = _dbContext.UpdateCategory(category);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_UPDATE_FAILED",
                            "La mise à jour de la catégorie a échoué.");
                    }

                    InvalidateCategoryCache(category.UserId.Value);
                    return ServiceResult<Category>.Success(category, "Catégorie mise à jour avec succès.");
                },
                operationName: nameof(UpdateCategoryAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour de la catégorie.");
        }

        public Task<ServiceResult<Category>> CustomizeSystemCategoryAsync(Category category)
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

                    int userId = category.UserId.Value;
                    Category? systemCategory = _dbContext.GetCategoryById(category.Id);
                    if (systemCategory is null || !systemCategory.IsSystem)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_SYSTEM_NOT_FOUND",
                            "Catégorie système introuvable.");
                    }

                    Category? existingOverride = _dbContext.GetCategoryOverride(userId, systemCategory.Id);
                    ServiceResult normalizationResult = NormalizeAndValidateCategory(category, existingOverride ?? systemCategory);
                    if (!normalizationResult.IsSuccess)
                    {
                        return ServiceResult<Category>.Failure(
                            normalizationResult.ErrorCode,
                            normalizationResult.Message);
                    }

                    ServiceResult<bool> nameExistsResult = CategoryNameExistsInternal(
                        userId,
                        category.Name,
                        existingOverride?.Id,
                        systemCategory.Id);

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

                    Category userCategory = existingOverride ?? new Category
                    {
                        UserId = userId,
                        ParentCategoryId = systemCategory.Id,
                        IsSystem = false,
                        DisplayOrder = systemCategory.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    };

                    userCategory.Name = category.Name;
                    userCategory.Description = category.Description;
                    userCategory.Color = category.Color;
                    userCategory.Icon = category.Icon;
                    userCategory.IsActive = true;
                    userCategory.IsSystem = false;
                    userCategory.UserId = userId;
                    userCategory.ParentCategoryId = systemCategory.Id;

                    if (existingOverride is null)
                    {
                        int newCategoryId = _dbContext.InsertCategory(userCategory);
                        if (newCategoryId <= 0)
                        {
                            return ServiceResult<Category>.Failure(
                                "CATEGORY_OVERRIDE_CREATE_FAILED",
                                "Impossible de personnaliser la catégorie système.");
                        }

                        userCategory.Id = newCategoryId;
                    }
                    else if (_dbContext.UpdateCategory(userCategory) != 1)
                    {
                        return ServiceResult<Category>.Failure(
                            "CATEGORY_OVERRIDE_UPDATE_FAILED",
                            "La mise à jour de la catégorie personnalisée a échoué.");
                    }

                    _dbContext.MigrateCategoryUsageForUser(userId, systemCategory.Id, userCategory.Id);
                    InvalidateCategoryCache(userId);

                    return ServiceResult<Category>.Success(userCategory, "Catégorie personnalisée avec succès.");
                },
                operationName: nameof(CustomizeSystemCategoryAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la personnalisation de la catégorie.");
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

                    InvalidateCategoryCache(userId);
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

                    InvalidateCategoryCache(userId);
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

                    InvalidateCategoryCache(userId);
                    return ServiceResult.Success("Ordre des catégories mis à jour avec succès.");
                },
                operationName: nameof(ReorderCategoriesAsync),
                fallbackErrorCode: "CATEGORY_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du réordonnancement des catégories.");
        }

        private ServiceResult<bool> CategoryNameExistsInternal(
            int userId,
            string categoryName,
            int? excludedCategoryId = null,
            int? excludedParentCategoryId = null)
        {
            if (userId <= 0)
                return ServiceResult<bool>.Failure("CATEGORY_INVALID_USER", ServiceMessages.InvalidUser);

            if (string.IsNullOrWhiteSpace(categoryName))
                return ServiceResult<bool>.Failure("CATEGORY_NAME_REQUIRED", "Le nom de la catégorie est requis.");

            string normalizedCategoryName = categoryName.Trim();

            bool exists = _dbContext.GetAllCategoriesByUserId(userId)
                .Where(category => !excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value)
                .Where(category => !excludedParentCategoryId.HasValue || category.Id != excludedParentCategoryId.Value)
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
            return _dbContext.GetAllCategoriesByUserId(userId)
                .Where(category => !category.IsSystem && category.UserId == userId)
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

        private List<Category> GetCachedVisibleCategories(int userId, bool includeInactive)
        {
            if (includeInactive)
                return _dbContext.GetAllCategoriesByUserId(userId);

            lock (_cacheLock)
            {
                if (_visibleCategoryCache.TryGetValue(userId, out List<Category>? cachedCategories))
                    return cachedCategories.Select(CloneCategory).ToList();
            }

            List<Category> categories = _dbContext.GetCategoriesByUserId(userId);

            lock (_cacheLock)
                _visibleCategoryCache[userId] = categories.Select(CloneCategory).ToList();

            return categories;
        }

        private void InvalidateCategoryCache(int userId)
        {
            lock (_cacheLock)
                _visibleCategoryCache.Remove(userId);
        }

        private static Category CloneCategory(Category category)
            => new()
            {
                Id = category.Id,
                UserId = category.UserId,
                ParentCategoryId = category.ParentCategoryId,
                IsSystem = category.IsSystem,
                Name = category.Name,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

        private static List<CategoryListItemDto> BuildCategoryListItems(
            List<Category> categories,
            List<AlertThreshold> alertThresholds,
            List<Budget> budgets,
            List<Expense> expenses)
        {
            DateTime now = DateTime.Now;
            DateTime periodStart = new(now.Year, now.Month, 1);
            DateTime periodEnd = periodStart.AddMonths(1).AddTicks(-1);

            Budget? globalBudget = budgets
                .Where(budget => budget.IsActive && budget.CategoryId == 0 && IsDateInBudgetPeriod(now, budget))
                .OrderByDescending(budget => budget.CreatedAt)
                .FirstOrDefault();

            Dictionary<int, Budget> categoryBudgets = budgets
                .Where(budget => budget.IsActive && budget.CategoryId > 0 && IsDateInBudgetPeriod(now, budget))
                .GroupBy(budget => budget.CategoryId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(budget => budget.CreatedAt).First());

            Dictionary<int, AlertThreshold> categoryAlerts = alertThresholds
                .Where(alert => alert.IsActive && alert.CategoryId.HasValue && !alert.BudgetId.HasValue)
                .GroupBy(alert => alert.CategoryId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(alert => alert.ThresholdPercentage).First());

            return categories
                .Select(category =>
                {
                    int effectiveParentId = category.ParentCategoryId ?? category.Id;
                    Budget? categoryBudget = categoryBudgets.GetValueOrDefault(category.Id)
                        ?? categoryBudgets.GetValueOrDefault(effectiveParentId);
                    decimal budgetAmount = categoryBudget?.Amount ?? globalBudget?.Amount ?? 0m;

                    AlertThreshold? alert = categoryAlerts.GetValueOrDefault(category.Id)
                        ?? categoryAlerts.GetValueOrDefault(effectiveParentId);
                    decimal thresholdPercentage = alert?.ThresholdPercentage ?? 100m;
                    decimal thresholdAmount = budgetAmount > 0
                        ? budgetAmount * thresholdPercentage / 100m
                        : 0m;

                    decimal spentAmount = expenses
                        .Where(expense =>
                            expense.DateOperation >= periodStart &&
                            expense.DateOperation <= periodEnd &&
                            (expense.CategoryId == category.Id || expense.CategoryId == effectiveParentId))
                        .Sum(expense => expense.Amount);

                    decimal consumedPercentage = budgetAmount > 0
                        ? spentAmount / budgetAmount * 100m
                        : 0m;
                    decimal remainingBeforeThreshold = thresholdAmount - spentAmount;

                    return new CategoryListItemDto
                    {
                        Category = category,
                        BudgetAmount = budgetAmount,
                        SpentAmount = spentAmount,
                        ThresholdPercentage = thresholdPercentage,
                        ThresholdAmount = thresholdAmount,
                        RemainingBeforeThreshold = remainingBeforeThreshold,
                        ConsumedPercentage = consumedPercentage,
                        HasAlertThreshold = alert is not null,
                        ThresholdStatus = ResolveThresholdStatus(spentAmount, thresholdAmount)
                    };
                })
                .ToList();
        }

        private static bool IsDateInBudgetPeriod(DateTime date, Budget budget)
        {
            DateTime startDate = new(budget.StartDate.Year, budget.StartDate.Month, 1);
            DateTime endDate = (budget.EndDate ?? startDate.AddMonths(1).AddDays(-1)).Date.AddDays(1).AddTicks(-1);
            return date >= startDate && date <= endDate;
        }

        private static string ResolveThresholdStatus(decimal spentAmount, decimal thresholdAmount)
        {
            if (thresholdAmount <= 0)
                return "Aucun seuil";

            decimal ratio = spentAmount / thresholdAmount;
            if (ratio >= 1m)
                return "Dépassé";

            return ratio >= 0.8m ? "Seuil proche" : "OK";
        }
    }
}
