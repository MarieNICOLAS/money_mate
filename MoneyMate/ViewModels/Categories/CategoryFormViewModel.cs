using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MoneyMate.ViewModels.Categories;

/// <summary>
/// Formulaire de création / édition d'une catégorie.
/// </summary>
public class CategoryFormViewModel : FormViewModelBase
{
    private readonly ICategoryService _categoryService;
    private readonly IAlertThresholdService _alertThresholdService;
    private readonly IAppEventBus _appEventBus;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _colorHex = "#6B7A8F";
    private string _icon = "💰";
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private bool _isSystemCategory;
    private bool _enableCategoryAlert;
    private string _categoryAlertThresholdPercentageText = "80";
    private string _categoryAlertMessage = string.Empty;

    public CategoryFormViewModel(
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
        Title = "Catégorie";
        SelectColorCommand = new Command<string>(SelectColor);
        SelectIconCommand = new Command<string>(SelectIcon);
        AvailableColorOptions = new(AvailableColors.Select(color => new CategorySelectionOptionViewModel(color, SelectColorCommand)).ToArray());
        AvailableIconOptions = new(AvailableIcons.Select(icon => new CategorySelectionOptionViewModel(icon, SelectIconCommand)).ToArray());
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

    public bool EnableCategoryAlert
    {
        get => _enableCategoryAlert;
        set => SetFormProperty(ref _enableCategoryAlert, value);
    }

    public string CategoryAlertThresholdPercentageText
    {
        get => _categoryAlertThresholdPercentageText;
        set => SetFormProperty(ref _categoryAlertThresholdPercentageText, value);
    }

    public string CategoryAlertMessage
    {
        get => _categoryAlertMessage;
        set => SetFormProperty(ref _categoryAlertMessage, value);
    }

    protected override string EditParameterKey => NavigationParameterKeys.CategoryId;

    protected override string? CancelNavigationFallbackRoute => AppRoutes.CategoriesList;

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
        EnableCategoryAlert = false;
        CategoryAlertThresholdPercentageText = "80";
        CategoryAlertMessage = string.Empty;
        _createdAt = DateTime.UtcNow;
        OnPropertyChanged(nameof(IsCustomCategory));
        OnPropertyChanged(nameof(CanEditSystemCategory));
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

        await LoadCategoryAlertAsync(category.Id);

        OnPropertyChanged(nameof(IsCustomCategory));
        OnPropertyChanged(nameof(CanEditSystemCategory));
    }

    protected override string ValidateForm()
    {
        if (!EnsureCurrentUser())
            return ErrorMessage;

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

        if (EnableCategoryAlert)
        {
            if (!TryParseDecimalInput(CategoryAlertThresholdPercentageText, out decimal threshold) || threshold < 0 || threshold > 100)
                return "Le seuil d'alerte de la catégorie doit être compris entre 0 et 100.";
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
            ? IsSystemCategory
                ? await _categoryService.CustomizeSystemCategoryAsync(category)
                : await _categoryService.UpdateCategoryAsync(category)
            : await _categoryService.CreateCategoryAsync(category);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        Category savedCategory = result.Data ?? category;
        if (!await SyncCategoryAlertAsync(savedCategory.Id, savedCategory.Name))
            return false;

        _appEventBus.PublishDataChanged(AppDataChangeKind.Categories | AppDataChangeKind.AlertThresholds);
        await NavigationService.NavigateToAsync(AppRoutes.CategoriesList);
        return true;
    }
    private void SelectColor(string? colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
            return;

        ColorHex = colorHex;
    }

    private void SelectIcon(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
            return;

        Icon = icon;
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

        _appEventBus.PublishDataChanged(AppDataChangeKind.Categories | AppDataChangeKind.AlertThresholds);
        await NavigationService.NavigateToAsync(AppRoutes.CategoriesList);
        return true;
    }
    public ReadOnlyCollection<string> AvailableColors { get; } =
    new(new[]
    {
        "#6B7A8F",
        "#6793AE",
        "#26658C",
        "#4F7993",
        "#6CC57C",
        "#F6B092",
        "#E58DA3",
        "#E57373",
        "#9C89B8",
        "#F4C95D"
    });

    public ReadOnlyCollection<string> AvailableIcons { get; } =
        new(new[]
        {
        "💰", "🛒", "🍔", "🏠", "🚗",
        "🎉", "🎓", "💊", "🐶", "📱",
        "🧾", "✈️", "🎁", "👕", "⚡"
        });

    public ICommand SelectColorCommand { get; }
    public ICommand SelectIconCommand { get; }

    public ReadOnlyCollection<CategorySelectionOptionViewModel> AvailableColorOptions { get; }

    public ReadOnlyCollection<CategorySelectionOptionViewModel> AvailableIconOptions { get; }

    public bool CanEditSystemCategory => !IsSystemCategory;

    private async Task LoadCategoryAlertAsync(int categoryId)
    {
        var alertsResult = await _alertThresholdService.GetAlertThresholdsAsync(CurrentUserId);
        if (!alertsResult.IsSuccess)
        {
            ErrorMessage = alertsResult.Message;
            EnableCategoryAlert = false;
            CategoryAlertThresholdPercentageText = "80";
            CategoryAlertMessage = string.Empty;
            return;
        }

        AlertThreshold? existingCategoryAlert = (alertsResult.Data ?? [])
            .FirstOrDefault(alert => alert.CategoryId == categoryId && !alert.BudgetId.HasValue);

        EnableCategoryAlert = existingCategoryAlert is not null;
        CategoryAlertThresholdPercentageText = existingCategoryAlert?.ThresholdPercentage.ToString("0.##") ?? "80";
        CategoryAlertMessage = existingCategoryAlert?.Message ?? string.Empty;
    }

    private async Task<bool> SyncCategoryAlertAsync(int categoryId, string categoryName)
    {
        var alertsResult = await _alertThresholdService.GetAlertThresholdsAsync(CurrentUserId);
        if (!alertsResult.IsSuccess)
        {
            ErrorMessage = alertsResult.Message;
            return false;
        }

        AlertThreshold? existingCategoryAlert = (alertsResult.Data ?? [])
            .FirstOrDefault(alert => alert.CategoryId == categoryId && !alert.BudgetId.HasValue);

        if (!EnableCategoryAlert)
        {
            if (existingCategoryAlert is null)
                return true;

            var deleteResult = await _alertThresholdService.DeleteAlertThresholdAsync(existingCategoryAlert.Id, CurrentUserId);
            if (!deleteResult.IsSuccess)
            {
                ErrorMessage = deleteResult.Message;
                return false;
            }

            return true;
        }

        _ = TryParseDecimalInput(CategoryAlertThresholdPercentageText, out decimal threshold);

        AlertThreshold alertThreshold = new()
        {
            Id = existingCategoryAlert?.Id ?? 0,
            UserId = CurrentUserId,
            BudgetId = null,
            CategoryId = categoryId,
            ThresholdPercentage = threshold,
            AlertType = existingCategoryAlert?.AlertType ?? "Warning",
            Message = string.IsNullOrWhiteSpace(CategoryAlertMessage)
                ? $"Seuil atteint pour la catégorie {categoryName}."
                : CategoryAlertMessage.Trim(),
            IsActive = existingCategoryAlert?.IsActive ?? true,
            SendNotification = existingCategoryAlert?.SendNotification ?? true,
            CreatedAt = existingCategoryAlert?.CreatedAt ?? DateTime.UtcNow
        };

        var saveAlertResult = existingCategoryAlert is null
            ? await _alertThresholdService.CreateAlertThresholdAsync(alertThreshold)
            : await _alertThresholdService.UpdateAlertThresholdAsync(alertThreshold);

        if (!saveAlertResult.IsSuccess)
        {
            ErrorMessage = saveAlertResult.Message;
            return false;
        }

        return true;
    }
}

public sealed class CategorySelectionOptionViewModel
{
    public CategorySelectionOptionViewModel(string value, ICommand selectCommand)
    {
        Value = value;
        SelectCommand = new Command(() => selectCommand.Execute(Value));
    }

    public string Value { get; }

    public ICommand SelectCommand { get; }
}
