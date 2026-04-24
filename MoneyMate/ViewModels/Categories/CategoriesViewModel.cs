using System.Collections.ObjectModel;
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
    private readonly ICategoryService _categoryService;

    public CategoriesViewModel(
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        Title = "Catégories";
        Categories = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        AddCategoryCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AddCategory));
        EditCategoryCommand = new Command<CategoryItemViewModel>(async category => await EditCategoryAsync(category));
        ToggleCategoryActiveCommand = new Command<CategoryItemViewModel>(async category => await ToggleCategoryActiveAsync(category));
        DeleteCategoryCommand = new Command<CategoryItemViewModel>(async category => await DeleteCategoryAsync(category));
    }

    public ObservableCollection<CategoryItemViewModel> Categories { get; }

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
        await ExecuteBusyActionAsync(async () =>
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

            IEnumerable<Category> orderedCategories = (activeCategoriesResult.Data ?? [])
                .Concat(inactiveCategoriesResult.Data ?? [])
                .GroupBy(category => category.Id)
                .Select(group => group.First())
                .OrderByDescending(category => category.IsActive)
                .ThenByDescending(category => category.IsSystem)
                .ThenBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name);

            Categories.Clear();
            foreach (Category category in orderedCategories)
                Categories.Add(CategoryItemViewModel.FromModel(category));

            RefreshState();
        }, "Une erreur est survenue lors du chargement des catégories.");
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

            await LoadAsync();
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

            await LoadAsync();
        }, "Une erreur est survenue lors de la suppression de la catégorie.");
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(ActiveCategoriesCount));
        OnPropertyChanged(nameof(CustomCategoriesCount));
        OnPropertyChanged(nameof(InactiveCategoriesCount));
        OnPropertyChanged(nameof(HasCategories));
    }
}

/// <summary>
/// Représentation UI d'une catégorie.
/// </summary>
public sealed class CategoryItemViewModel
{
    private static readonly Color DefaultColor = Color.FromArgb("#6B7A8F");

    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Icon { get; init; } = "💰";

    public Color CategoryColor { get; init; } = DefaultColor;

    public bool IsSystem { get; init; }

    public bool IsActive { get; init; }

    public bool CanManage => !IsSystem;
    public bool CanEdit => !IsSystem;
    public bool CanDelete => !IsSystem;
    public bool CanToggleActive => !IsSystem;

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public string TypeText => IsSystem ? "Système" : "Personnalisée";

    public string StatusText => IsActive ? "Active" : "Inactive";

    public string ToggleActionText => IsActive ? "Désactiver" : "Activer";

    public static CategoryItemViewModel FromModel(Category category)
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
            IsActive = category.IsActive
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
}
