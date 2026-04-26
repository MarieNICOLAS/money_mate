using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Categories;

/// <summary>
/// ViewModel de consultation et de gestion des catégories utilisateur.
/// </summary>
public class CategoriesViewModel : AuthenticatedViewModelBase
{
    private const AppDataChangeKind RefreshChangeKinds = AppDataChangeKind.Categories | AppDataChangeKind.AlertThresholds;

    private readonly ICategoryService _categoryService;
    private readonly IAlertThresholdService _alertThresholdService;
    private readonly IAppEventBus _appEventBus;
    private string _selectedSortOption = SortAlphabetical;
    private long _lastRefreshVersion = -1;

    private const string SortAlphabetical = "Alphabétique";
    private const string SortAlertThreshold = "Alerte %";

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
        _alertThresholdService = alertThresholdService ?? throw new ArgumentNullException(nameof(alertThresholdService));
        _appEventBus = appEventBus ?? NullAppEventBus.Instance;

        Title = "Catégories";
        Categories = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        AddCategoryCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AddCategory));
        EditCategoryCommand = new Command<CategoryItemViewModel>(async category => await EditCategoryAsync(category));
        ToggleCategoryActiveCommand = new Command<CategoryItemViewModel>(async category => await ToggleCategoryActiveAsync(category));
        DeleteCategoryCommand = new Command<CategoryItemViewModel>(async category => await DeleteCategoryAsync(category));
    }

    public ObservableCollection<CategoryItemViewModel> Categories { get; }

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

        var activeCategoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);
        if (!activeCategoriesResult.IsSuccess)
        {
            ErrorMessage = activeCategoriesResult.Message;
            RefreshState();
            return;
        }

        var inactiveCategoriesResult = await _categoryService.GetInactiveCategoriesAsync(CurrentUserId);
        if (!inactiveCategoriesResult.IsSuccess)
        {
            ErrorMessage = inactiveCategoriesResult.Message;
            RefreshState();
            return;
        }

        Dictionary<int, AlertThreshold> categoryAlerts = [];
        var alertThresholdsResult = await _alertThresholdService.GetAlertThresholdsAsync(CurrentUserId);
        if (alertThresholdsResult.IsSuccess)
        {
            categoryAlerts = (alertThresholdsResult.Data ?? [])
                .Where(alert => alert.CategoryId.HasValue && !alert.BudgetId.HasValue && alert.IsActive)
                .GroupBy(alert => alert.CategoryId!.Value)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(alert => alert.ThresholdPercentage).First());
        }

        List<CategoryItemViewModel> categoryItems = (activeCategoriesResult.Data ?? [])
            .Concat(inactiveCategoriesResult.Data ?? [])
            .GroupBy(category => category.Id)
            .Select(group => group.First())
            .Select(category => CategoryItemViewModel.FromModel(
                category,
                categoryAlerts.TryGetValue(category.Id, out AlertThreshold? alertThreshold)
                    ? alertThreshold
                    : null))
            .ToList();

        Categories.Clear();
        foreach (CategoryItemViewModel category in OrderCategories(categoryItems))
        {
            category.PropertyChanged += OnCategoryItemPropertyChanged;
            Categories.Add(category);
        }

        RefreshState();
        UpdateObservedRefreshVersion();
    }

    private async Task EditCategoryAsync(CategoryItemViewModel? category)
    {
        if (category == null || category.IsSystem)
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

    private async Task ToggleCategoryActiveAsync(CategoryItemViewModel? category)
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

    private async Task DeleteCategoryAsync(CategoryItemViewModel? category)
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

        List<CategoryItemViewModel> orderedCategories = OrderCategories(Categories).ToList();
        for (int index = 0; index < orderedCategories.Count; index++)
        {
            CategoryItemViewModel category = orderedCategories[index];
            int currentIndex = Categories.IndexOf(category);
            if (currentIndex != index)
                Categories.Move(currentIndex, index);
        }
    }

    private IEnumerable<CategoryItemViewModel> OrderCategories(IEnumerable<CategoryItemViewModel> categories)
    {
        IOrderedEnumerable<CategoryItemViewModel> orderedCategories = SelectedSortOption == SortAlertThreshold
            ? categories
                .OrderByDescending(category => category.HasAlertThreshold)
                .ThenByDescending(category => category.AlertThresholdPercentage ?? decimal.MinValue)
            : categories
                .OrderBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase);

        return orderedCategories
            .ThenByDescending(category => category.IsActive)
            .ThenByDescending(category => category.IsSystem)
            .ThenBy(category => category.DisplayName, StringComparer.CurrentCultureIgnoreCase);
    }

    private void OnCategoryItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CategoryItemViewModel.IsActive))
            RefreshState();
    }
}

/// <summary>
/// Représentation UI d'une catégorie.
/// </summary>
public sealed class CategoryItemViewModel : INotifyPropertyChanged
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

    public decimal? AlertThresholdPercentage { get; init; }

    public bool HasAlertThreshold => AlertThresholdPercentage.HasValue;

    public string AlertThresholdText => HasAlertThreshold
        ? $"Alerte {AlertThresholdPercentage:0.##}%"
        : "Aucune alerte";

    public string DisplayName => Name ?? string.Empty;

    public bool CanManage => !IsSystem;
    public bool CanEdit => !IsSystem;
    public bool CanDelete => !IsSystem;
    public bool CanToggleActive => !IsSystem;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public string TypeText => IsSystem ? "Système" : "Personnalisée";

    public string StatusText => IsActive ? "Active" : "Inactive";

    public IReadOnlyList<string> Tags => BuildTags();

    public string ToggleActionText => IsActive ? "Off" : "On";

    public string ToggleActionIcon => IsActive ? "\uE9F5" : "\uE9F6";

    public event PropertyChangedEventHandler? PropertyChanged;

    public static CategoryItemViewModel FromModel(Category category, AlertThreshold? alertThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryItemViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Icon = string.IsNullOrWhiteSpace(category.Icon) ? "💰" : category.Icon,
            CategoryColor = TryCreateColor(category.Color),
            IsSystem = category.IsSystem,
            IsActive = category.IsActive,
            AlertThresholdPercentage = alertThreshold?.ThresholdPercentage
        };
    }

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

    private IReadOnlyList<string> BuildTags()
    {
        List<string> tags =
        [
            TypeText,
            StatusText
        ];

        if (HasAlertThreshold)
            tags.Add(AlertThresholdText);

        return tags;
    }
}
