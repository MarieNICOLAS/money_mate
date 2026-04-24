using System.Collections.ObjectModel;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;

namespace MoneyMate.ViewModels.Alerts;

/// <summary>
/// Formulaire de création / édition d'un seuil d'alerte.
/// </summary>
public class AlertThresholdFormViewModel : FormViewModelBase
{
    private readonly IAlertThresholdService _alertThresholdService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;
    private int _selectedBudgetId;
    private int _selectedCategoryId;
    private string _thresholdPercentageText = "80";
    private string _selectedAlertType = "Warning";
    private string _message = string.Empty;
    private bool _isActive = true;
    private bool _sendNotification = true;
    private DateTime _createdAt = DateTime.UtcNow;

    public AlertThresholdFormViewModel(
        IAlertThresholdService alertThresholdService,
        IBudgetService budgetService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _alertThresholdService = alertThresholdService ?? throw new ArgumentNullException(nameof(alertThresholdService));
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        Budgets = [];
        Categories = [];
        AlertTypes = ["Warning", "Critical"];
        Title = "Alerte";
        RefreshFormState();
    }

    public ObservableCollection<BudgetOptionViewModel> Budgets { get; }

    public ObservableCollection<CategoryOptionViewModel> Categories { get; }

    public IReadOnlyList<string> AlertTypes { get; }

    public int SelectedBudgetId
    {
        get => _selectedBudgetId;
        set => SetFormProperty(ref _selectedBudgetId, value);
    }

    public int SelectedCategoryId
    {
        get => _selectedCategoryId;
        set => SetFormProperty(ref _selectedCategoryId, value);
    }

    public string ThresholdPercentageText
    {
        get => _thresholdPercentageText;
        set => SetFormProperty(ref _thresholdPercentageText, value);
    }

    public string SelectedAlertType
    {
        get => _selectedAlertType;
        set => SetFormProperty(ref _selectedAlertType, value);
    }

    public string Message
    {
        get => _message;
        set => SetFormProperty(ref _message, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetFormProperty(ref _isActive, value);
    }

    public bool SendNotification
    {
        get => _sendNotification;
        set => SetFormProperty(ref _sendNotification, value);
    }

    protected override string EditParameterKey => NavigationParameterKeys.AlertThresholdId;

    protected override async Task LoadLookupsAsync()
    {
        var categoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);
        if (!categoriesResult.IsSuccess)
        {
            ErrorMessage = categoriesResult.Message;
            Categories.Clear();
        }
        else
        {
            Categories.Clear();
            foreach (Category category in (categoriesResult.Data ?? []).OrderBy(category => category.Name))
                Categories.Add(CategoryOptionViewModel.FromModel(category));
        }

        var budgetsResult = await _budgetService.GetBudgetsAsync(CurrentUserId);
        if (!budgetsResult.IsSuccess)
        {
            if (string.IsNullOrWhiteSpace(ErrorMessage))
                ErrorMessage = budgetsResult.Message;

            Budgets.Clear();
            return;
        }

        Budgets.Clear();
        foreach (Budget budget in (budgetsResult.Data ?? []).OrderByDescending(budget => budget.StartDate))
            Budgets.Add(BudgetOptionViewModel.FromModel(budget));
    }

    protected override Task InitializeForCreateAsync()
    {
        Title = "Nouvelle alerte";
        SelectedBudgetId = 0;
        SelectedCategoryId = 0;
        ThresholdPercentageText = "80";
        SelectedAlertType = "Warning";
        Message = string.Empty;
        IsActive = true;
        SendNotification = true;
        _createdAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    protected override async Task LoadForEditAsync(int entityId)
    {
        var result = await _alertThresholdService.GetAlertThresholdByIdAsync(entityId, CurrentUserId);
        if (!result.IsSuccess || result.Data == null)
        {
            ErrorMessage = result.Message;
            return;
        }

        AlertThreshold alert = result.Data;
        Title = "Modifier l'alerte";
        SelectedBudgetId = alert.BudgetId ?? 0;
        SelectedCategoryId = alert.CategoryId ?? 0;
        ThresholdPercentageText = alert.ThresholdPercentage.ToString("0.##");
        SelectedAlertType = alert.AlertType;
        Message = alert.Message;
        IsActive = alert.IsActive;
        SendNotification = alert.SendNotification;
        _createdAt = alert.CreatedAt;
    }

    protected override string ValidateForm()
    {
        if (CurrentUserId <= 0)
            return "Aucune session utilisateur active.";

        if (!TryParseDecimalInput(ThresholdPercentageText, out decimal threshold) || threshold < 0 || threshold > 100)
            return "Le seuil doit être compris entre 0 et 100.";

        if (string.IsNullOrWhiteSpace(SelectedAlertType) || !AlertTypes.Contains(SelectedAlertType))
            return "Le type d'alerte est invalide.";

        if (SelectedBudgetId <= 0 && SelectedCategoryId <= 0)
            return "Le seuil d'alerte doit cibler un budget ou une catégorie.";

        return string.Empty;
    }

    protected override async Task<bool> SaveCoreAsync()
    {
        _ = TryParseDecimalInput(ThresholdPercentageText, out decimal threshold);

        AlertThreshold alertThreshold = new()
        {
            Id = EditingEntityId,
            UserId = CurrentUserId,
            BudgetId = SelectedBudgetId > 0 ? SelectedBudgetId : null,
            CategoryId = SelectedCategoryId > 0 ? SelectedCategoryId : null,
            ThresholdPercentage = threshold,
            AlertType = SelectedAlertType,
            Message = Message,
            IsActive = IsActive,
            SendNotification = SendNotification,
            CreatedAt = _createdAt
        };

        var result = IsEditMode
            ? await _alertThresholdService.UpdateAlertThresholdAsync(alertThreshold)
            : await _alertThresholdService.CreateAlertThresholdAsync(alertThreshold);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync(AppRoutes.AlertThreshold);
        return true;
    }

    protected override async Task<bool> DeleteCoreAsync()
    {
        if (!IsEditMode)
            return false;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Suppression",
            "Supprimer ce seuil d'alerte ?",
            "Oui",
            "Non");

        if (!confirm)
            return false;

        var result = await _alertThresholdService.DeleteAlertThresholdAsync(EditingEntityId, CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync(AppRoutes.AlertThreshold);
        return true;
    }
}
