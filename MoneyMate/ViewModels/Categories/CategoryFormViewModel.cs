using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;

namespace MoneyMate.ViewModels.Categories;

/// <summary>
/// Formulaire de création / édition d'une catégorie.
/// </summary>
public class CategoryFormViewModel : FormViewModelBase
{
    private readonly ICategoryService _categoryService;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _colorHex = "#6B7A8F";
    private string _icon = "💰";
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private bool _isSystemCategory;

    public CategoryFormViewModel(
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        Title = "Catégorie";
        RefreshFormState();
    }

    public string Name
    {
        get => _name;
        set => SetFormProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetFormProperty(ref _description, value);
    }

    public string ColorHex
    {
        get => _colorHex;
        set => SetFormProperty(ref _colorHex, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetFormProperty(ref _icon, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetFormProperty(ref _isActive, value);
    }

    public bool IsSystemCategory
    {
        get => _isSystemCategory;
        private set => SetProperty(ref _isSystemCategory, value);
    }

    public bool IsCustomCategory => !IsSystemCategory;

    protected override string EditParameterKey => NavigationParameterKeys.CategoryId;

    protected override bool CanDeleteEntity => IsEditMode && !IsSystemCategory;

    protected override Task InitializeForCreateAsync()
    {
        Title = "Nouvelle catégorie";
        Name = string.Empty;
        Description = string.Empty;
        ColorHex = "#6B7A8F";
        Icon = "💰";
        IsActive = true;
        IsSystemCategory = false;
        _createdAt = DateTime.UtcNow;
        OnPropertyChanged(nameof(IsCustomCategory));
        return Task.CompletedTask;
    }

    protected override async Task LoadForEditAsync(int entityId)
    {
        var result = await _categoryService.GetCategoryByIdAsync(entityId, CurrentUserId);
        if (!result.IsSuccess || result.Data == null)
        {
            ErrorMessage = result.Message;
            return;
        }

        Category category = result.Data;
        Title = "Modifier la catégorie";
        Name = category.Name;
        Description = category.Description;
        ColorHex = category.Color;
        Icon = category.Icon;
        IsActive = category.IsActive;
        IsSystemCategory = category.IsSystem;
        _createdAt = category.CreatedAt;
        OnPropertyChanged(nameof(IsCustomCategory));
    }

    protected override string ValidateForm()
    {
        if (!EnsureCurrentUser())
            return ErrorMessage;

        if (IsSystemCategory)
            return "Cette catégorie système ne peut pas être modifiée depuis le formulaire.";

        if (string.IsNullOrWhiteSpace(Name))
            return "Le nom de la catégorie est requis.";

        if (string.IsNullOrWhiteSpace(ColorHex))
            return "La couleur de la catégorie est requise.";

        try
        {
            _ = Microsoft.Maui.Graphics.Color.FromArgb(ColorHex.Trim());
        }
        catch
        {
            return "La couleur doit être au format hexadécimal.";
        }

        return string.Empty;
    }

    protected override async Task<bool> SaveCoreAsync()
    {
        Category category = new()
        {
            Id = EditingEntityId,
            UserId = CurrentUserId,
            Name = Name.Trim(),
            Description = Description.Trim(),
            Color = ColorHex.Trim(),
            Icon = string.IsNullOrWhiteSpace(Icon) ? "💰" : Icon.Trim(),
            IsActive = IsActive,
            CreatedAt = _createdAt,
            IsSystem = IsSystemCategory
        };

        var result = IsEditMode
            ? await _categoryService.UpdateCategoryAsync(category)
            : await _categoryService.CreateCategoryAsync(category);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync("//CategoriesListPage");
        return true;
    }

    protected override async Task<bool> DeleteCoreAsync()
    {
        if (!IsEditMode || IsSystemCategory)
            return false;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Suppression",
            $"Supprimer la catégorie '{Name}' ?",
            "Oui",
            "Non");

        if (!confirm)
            return false;

        var result = await _categoryService.DeleteCategoryAsync(EditingEntityId, CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync("//CategoriesListPage");
        return true;
    }
}
