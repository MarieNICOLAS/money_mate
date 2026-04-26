using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MoneyMate.Configuration;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;

namespace MoneyMate.ViewModels.Categories;

/// <summary>
/// ViewModel de consultation et de gestion des catégories utilisateur.
/// </summary>
public class CategoriesViewModel : AuthenticatedViewModelBase
{
    private const AppDataChangeKind RefreshChangeKinds =
        AppDataChangeKind.Categories |
        AppDataChangeKind.AlertThresholds |
        AppDataChangeKind.Expenses |
        AppDataChangeKind.Budgets;

    private const string SortAlphabetical = "Alphabétique";
    private const string SortAlertThreshold = "Alerte %";

    private readonly ICategoryService _categoryService;
    private readonly IAppEventBus _appEventBus;
    private string _selectedSortOption = SortAlphabetical;
    private long _lastRefreshVersion = -1;

    public CategoriesViewModel(
        ICategoryService categoryService,
        IAlertThresholdService alertThresholdService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService,
        IAppEventBus? appEventBus = null)
        : base(authenticationService, dialogService, navigationService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _ = alertThresholdService ?? throw new ArgumentNullException(nameof(alertThresholdService));
        _appEventBus = appEventBus ?? NullAppEventBus.Instance;

        Title = "Catégories";
        Categories = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        AddCategoryCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AddCategory));
        EditCategoryCommand = new Command<CategoryListItemViewModel>(async category => await EditCategoryAsync(category));
        ToggleCategoryActiveCommand = new Command<CategoryListItemViewModel>(async category => await ToggleCategoryActiveAsync(category));
        DeleteCategoryCommand = new Command<CategoryListItemViewModel>(async category => await DeleteCategoryAsync(category));
    }

    public ObservableCollection<CategoryListItemViewModel> Categories { get; }

    public ReadOnlyCollection<string> SortOptions { get; } = new([SortAlphabetical, SortAlertThreshold]);

    public string SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (SetProperty(ref _selectedSortOption, value))
                ApplySorting();
        }
    }

    public ICommand RefreshCommand { get; }

    public ICommand AddCategoryCommand { get; }

    public ICommand EditCategoryCommand { get; }

    public ICommand ToggleCategoryActiveCommand { get; }

    public ICommand DeleteCategoryCommand { get; }

    public int ActiveCategoriesCount => Categories.Count(category => category.IsActive);

    public int CustomCategoriesCount => Categories.Count(category => !category.IsSystem);

    public int InactiveCategoriesCount => Categories.Count(category => !category.IsActive);

    public bool HasCategories => Categories.Count > 0;

    public async Task LoadAsync()
    {
        await ExecuteBusyActionAsync(LoadCoreAsync, "Une erreur est survenue lors du chargement des catégories.");
    }

    public async Task RefreshIfNeededAsync()
    {
        if (_lastRefreshVersion < 0 || _appEventBus.HasChangedSince(RefreshChangeKinds, _lastRefreshVersion))
            await LoadAsync();
    }

    private async Task LoadCoreAsync()
    {
        if (!EnsureCurrentUser())
            return;

        var categoriesResult = await _categoryService.GetCategoryListItemsAsync(CurrentUserId);
        if (!categoriesResult.IsSuccess)
        {
            ErrorMessage = categoriesResult.Message;
            RefreshState();
            return;
        }

        List<CategoryListItemViewModel> categoryItems = (categoriesResult.Data ?? [])
            .Select(item => CategoryListItemViewModel.FromDto(item, CurrentDevise))
            .ToList();

        Categories.Clear();
        foreach (CategoryListItemViewModel category in OrderCategories(categoryItems))
            Categories.Add(category);

        RefreshState();
        UpdateObservedRefreshVersion();
    }

    private async Task EditCategoryAsync(CategoryListItemViewModel? category)
    {
        if (category == null)
            return;

        if (!EnsureCurrentUser())
            return;

        await NavigationService.NavigateToAsync(
            AppRoutes.EditCategory,
            new Dictionary<string, object>
            {
                [NavigationParameterKeys.CategoryId] = category.Id
            });
    }

    private async Task ToggleCategoryActiveAsync(CategoryListItemViewModel? category)
    {
        if (category == null || category.IsSystem)
            return;

        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            var result = await _categoryService.SetCategoryActiveStateAsync(category.Id, CurrentUserId, !category.IsActive);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return;
            }

            _appEventBus.PublishDataChanged(AppDataChangeKind.Categories);
            await LoadCoreAsync();
        }, "Une erreur est survenue lors de la mise à jour de la catégorie.");
    }

    private async Task DeleteCategoryAsync(CategoryListItemViewModel? category)
    {
        if (category == null || category.IsSystem)
            return;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Suppression",
            $"Supprimer la catégorie '{category.Name}' ?",
            "Oui",
            "Non");

        if (!confirm)
            return;

        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            var result = await _categoryService.DeleteCategoryAsync(category.Id, CurrentUserId);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return;
            }

            _appEventBus.PublishDataChanged(AppDataChangeKind.Categories);
            await LoadCoreAsync();
        }, "Une erreur est survenue lors de la suppression de la catégorie.");
    }

    private void UpdateObservedRefreshVersion()
        => _lastRefreshVersion = _appEventBus.GetVersion(RefreshChangeKinds);

    private void RefreshState()
    {
        OnPropertyChanged(nameof(ActiveCategoriesCount));
        OnPropertyChanged(nameof(CustomCategoriesCount));
        OnPropertyChanged(nameof(InactiveCategoriesCount));
        OnPropertyChanged(nameof(HasCategories));
    }

    private void ApplySorting()
    {
        if (Categories.Count <= 1)
            return;

        List<CategoryListItemViewModel> orderedCategories = OrderCategories(Categories).ToList();
        for (int index = 0; index < orderedCategories.Count; index++)
        {
            CategoryListItemViewModel category = orderedCategories[index];
            int currentIndex = Categories.IndexOf(category);
            if (currentIndex != index)
                Categories.Move(currentIndex, index);
        }
    }

    private IEnumerable<CategoryListItemViewModel> OrderCategories(IEnumerable<CategoryListItemViewModel> categories)
    {
        IOrderedEnumerable<CategoryListItemViewModel> orderedCategories = SelectedSortOption == SortAlertThreshold
            ? categories
                .OrderByDescending(category => category.HasAlertThreshold)
                .ThenByDescending(category => category.AlertThresholdPercentage)
            : categories
                .OrderBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase);

        return orderedCategories
            .ThenByDescending(category => category.IsActive)
            .ThenByDescending(category => category.IsSystem)
            .ThenBy(category => category.DisplayName, StringComparer.CurrentCultureIgnoreCase);
    }
}

/// <summary>
/// Représentation UI d'une catégorie avec données de seuil pré-calculées par le service.
/// </summary>
public sealed class CategoryListItemViewModel
{
    private static readonly Color DefaultColor = Color.FromArgb("#6B7A8F");
    private bool _isActive;

    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Icon { get; init; } = "💰";

    public Color CategoryColor { get; init; } = DefaultColor;

    public bool IsSystem { get; init; }

    public bool IsActive
    {
        get => _isActive;
        init => _isActive = value;
    }

    public decimal AlertThresholdPercentage { get; init; }

    public bool HasAlertThreshold { get; init; }

    public decimal BudgetAmount { get; init; }

    public decimal SpentAmount { get; init; }

    public decimal ThresholdAmount { get; init; }

    public decimal RemainingBeforeThreshold { get; init; }

    public decimal ConsumedPercentage { get; init; }

    public double GaugeProgress => ThresholdAmount > 0
        ? Math.Clamp((double)(SpentAmount / ThresholdAmount), 0d, 1d)
        : 0d;

    public Color ThresholdStatusColor { get; init; } = Color.FromArgb("#6CC57C");

    public string ThresholdStatusText { get; init; } = "OK";

    public string BudgetAmountText { get; init; } = string.Empty;

    public string SpentAmountText { get; init; } = string.Empty;

    public string ThresholdAmountText { get; init; } = string.Empty;

    public string RemainingBeforeThresholdText { get; init; } = string.Empty;

    public string ConsumedPercentageText => $"{ConsumedPercentage:0.#}% consommé";

    public string AlertThresholdText => HasAlertThreshold
        ? $"Seuil {AlertThresholdPercentage:0.##}%"
        : "Seuil 100%";

    public string DisplayName => Name ?? string.Empty;

    public bool CanEdit => true;

    public bool CanDelete => !IsSystem;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public string StatusText => IsActive ? "Active" : "Inactive";

    public static CategoryListItemViewModel FromDto(CategoryListItemDto dto, string devise)
    {
        ArgumentNullException.ThrowIfNull(dto);
        Category category = dto.Category;

        return new CategoryListItemViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Icon = string.IsNullOrWhiteSpace(category.Icon) ? "💰" : category.Icon,
            CategoryColor = TryCreateColor(category.Color),
            IsSystem = category.IsSystem,
            IsActive = category.IsActive,
            AlertThresholdPercentage = dto.ThresholdPercentage,
            HasAlertThreshold = dto.HasAlertThreshold,
            BudgetAmount = dto.BudgetAmount,
            SpentAmount = dto.SpentAmount,
            ThresholdAmount = dto.ThresholdAmount,
            RemainingBeforeThreshold = dto.RemainingBeforeThreshold,
            ConsumedPercentage = dto.ConsumedPercentage,
            ThresholdStatusText = dto.ThresholdStatus,
            ThresholdStatusColor = ResolveStatusColor(dto.ThresholdStatus),
            BudgetAmountText = CurrencyHelper.Format(dto.BudgetAmount, devise),
            SpentAmountText = CurrencyHelper.Format(dto.SpentAmount, devise),
            ThresholdAmountText = CurrencyHelper.Format(dto.ThresholdAmount, devise),
            RemainingBeforeThresholdText = CurrencyHelper.Format(Math.Max(0m, dto.RemainingBeforeThreshold), devise)
        };
    }

    private static Color ResolveStatusColor(string status)
        => status switch
        {
            "Dépassé" => Color.FromArgb("#E57373"),
            "Seuil proche" => Color.FromArgb("#FFB74D"),
            _ => Color.FromArgb("#6CC57C")
        };

    private static Color TryCreateColor(string? color)
    {
        if (!string.IsNullOrWhiteSpace(color))
        {
            try
            {
                return Color.FromArgb(color);
            }
            catch
            {
            }
        }

        return DefaultColor;
    }
}
