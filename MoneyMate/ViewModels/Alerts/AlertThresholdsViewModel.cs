using System.Collections.ObjectModel;
using System.Windows.Input;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;

namespace MoneyMate.ViewModels.Alerts;

/// <summary>
/// ViewModel de consultation des seuils d'alerte.
/// </summary>
public class AlertThresholdsViewModel : AuthenticatedViewModelBase
{
    private readonly IAlertThresholdService _alertThresholdService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;

    public AlertThresholdsViewModel(
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

        Title = "Alertes";
        Alerts = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        ToggleAlertActiveCommand = new Command<AlertThresholdItemViewModel>(async alert => await ToggleAlertActiveAsync(alert));
        EvaluateAlertCommand = new Command<AlertThresholdItemViewModel>(async alert => await EvaluateAlertAsync(alert));
    }

    public ObservableCollection<AlertThresholdItemViewModel> Alerts { get; }

    public ICommand RefreshCommand { get; }

    public ICommand ToggleAlertActiveCommand { get; }

    public ICommand EvaluateAlertCommand { get; }

    public int ActiveAlertsCount => Alerts.Count(alert => alert.IsActive);

    public int TriggeredAlertsCount => Alerts.Count(alert => alert.IsTriggered);

    public bool HasAlerts => Alerts.Count > 0;

    public async Task LoadAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            var alertsResult = await _alertThresholdService.GetAlertThresholdsAsync(CurrentUserId);
            if (!alertsResult.IsSuccess)
            {
                ErrorMessage = alertsResult.Message;
                RefreshState();
                return;
            }

            var budgetsResult = await _budgetService.GetBudgetsAsync(CurrentUserId);
            if (!budgetsResult.IsSuccess)
            {
                ErrorMessage = budgetsResult.Message;
                RefreshState();
                return;
            }

            var categoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);
            if (!categoriesResult.IsSuccess)
            {
                ErrorMessage = categoriesResult.Message;
                RefreshState();
                return;
            }

            Dictionary<int, Budget> budgetsById = (budgetsResult.Data ?? []).ToDictionary(budget => budget.Id, budget => budget);
            Dictionary<int, Category> categoriesById = (categoriesResult.Data ?? [])
                .GroupBy(category => category.Id)
                .Select(group => group.First())
                .ToDictionary(category => category.Id, category => category);

            List<AlertThresholdItemViewModel> items = [];
            foreach (AlertThreshold alert in (alertsResult.Data ?? []).OrderByDescending(alert => alert.ThresholdPercentage))
            {
                var evaluationResult = await _alertThresholdService.EvaluateAlertAsync(alert.Id, CurrentUserId);
                AlertTriggerInfo? triggerInfo = evaluationResult.IsSuccess ? evaluationResult.Data : null;

                items.Add(AlertThresholdItemViewModel.FromData(alert, triggerInfo, budgetsById, categoriesById));
            }

            Alerts.Clear();
            foreach (AlertThresholdItemViewModel item in items)
                Alerts.Add(item);

            RefreshState();
        }, "Une erreur est survenue lors du chargement des alertes.");
    }

    private async Task ToggleAlertActiveAsync(AlertThresholdItemViewModel? alert)
    {
        if (alert == null)
            return;

        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            var result = await _alertThresholdService.SetAlertThresholdActiveStateAsync(alert.Id, CurrentUserId, !alert.IsActive);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return;
            }

            await LoadAsync();
        }, "Une erreur est survenue lors de la mise à jour du seuil d'alerte.");
    }

    private async Task EvaluateAlertAsync(AlertThresholdItemViewModel? alert)
    {
        if (alert == null)
            return;

        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            var result = await _alertThresholdService.EvaluateAlertAsync(alert.Id, CurrentUserId);
            if (!result.IsSuccess || result.Data == null)
            {
                ErrorMessage = result.Message;
                return;
            }

            string message = result.Data.IsTriggered
                ? $"Seuil atteint à {result.Data.ConsumedPercentage:F0} % du budget."
                : $"Seuil non atteint : {result.Data.ConsumedPercentage:F0} % consommé.";

            await DialogService.ShowAlertAsync("Évaluation d'alerte", message, "OK");
            await LoadAsync();
        }, "Une erreur est survenue lors de l'évaluation de l'alerte.");
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(ActiveAlertsCount));
        OnPropertyChanged(nameof(TriggeredAlertsCount));
        OnPropertyChanged(nameof(HasAlerts));
    }
}

/// <summary>
/// Représentation UI d'un seuil d'alerte.
/// </summary>
public sealed class AlertThresholdItemViewModel
{
    public int Id { get; init; }

    public string TargetName { get; init; } = string.Empty;

    public string AlertTypeLabel { get; init; } = string.Empty;

    public decimal ThresholdPercentage { get; init; }

    public decimal CurrentPercentage { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool IsTriggered { get; init; }

    public bool IsActive { get; init; }

    public string StatusText => IsTriggered ? "Déclenchée" : "Sous contrôle";

    public string ToggleActionText => IsActive ? "Désactiver" : "Activer";

    public string ToggleActionIcon => IsActive ? "\uE9F5" : "\uE9F6";

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public static AlertThresholdItemViewModel FromData(
        AlertThreshold alertThreshold,
        AlertTriggerInfo? triggerInfo,
        IReadOnlyDictionary<int, Budget> budgetsById,
        IReadOnlyDictionary<int, Category> categoriesById)
    {
        ArgumentNullException.ThrowIfNull(alertThreshold);

        string targetName = BuildTargetName(alertThreshold, budgetsById, categoriesById);

        return new AlertThresholdItemViewModel
        {
            Id = alertThreshold.Id,
            TargetName = targetName,
            AlertTypeLabel = alertThreshold.AlertType.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                ? "Critique"
                : "Préventive",
            ThresholdPercentage = alertThreshold.ThresholdPercentage,
            CurrentPercentage = triggerInfo?.ConsumedPercentage ?? 0m,
            Message = alertThreshold.Message,
            IsTriggered = triggerInfo?.IsTriggered ?? false,
            IsActive = alertThreshold.IsActive
        };
    }

    private static string BuildTargetName(
        AlertThreshold alertThreshold,
        IReadOnlyDictionary<int, Budget> budgetsById,
        IReadOnlyDictionary<int, Category> categoriesById)
    {
        if (alertThreshold.BudgetId.HasValue && budgetsById.TryGetValue(alertThreshold.BudgetId.Value, out Budget? budget))
            return $"Budget • {budget.MonthLabel}";

        if (alertThreshold.CategoryId.HasValue && categoriesById.TryGetValue(alertThreshold.CategoryId.Value, out Category? targetCategory))
            return $"Catégorie • {targetCategory.Name}";

        return "Cible inconnue";
    }
}
