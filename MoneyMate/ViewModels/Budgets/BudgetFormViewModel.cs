using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;

namespace MoneyMate.ViewModels.Budgets;

/// <summary>
/// Formulaire de création / édition d'un budget mensuel global.
/// </summary>
public class BudgetFormViewModel : FormViewModelBase
{
    private readonly IBudgetService _budgetService;
    private readonly IAppEventBus _appEventBus;
    private string _amountText = string.Empty;
    private MonthOptionViewModel? _selectedMonth;
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private string _saveNavigationRoute = AppRoutes.BudgetsOverview;

    public BudgetFormViewModel(
        IBudgetService budgetService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService,
        IAppEventBus? appEventBus = null)
        : base(authenticationService, dialogService, navigationService)
    {
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _appEventBus = appEventBus ?? NullAppEventBus.Instance;
        MonthOptions = BuildMonthOptions();
        Title = "Budget";
        RefreshFormState();
    }

    public IReadOnlyList<MonthOptionViewModel> MonthOptions { get; }

    public string AmountText
    {
        get => _amountText;
        set => SetFormProperty(ref _amountText, value);
    }

    public MonthOptionViewModel? SelectedMonth
    {
        get => _selectedMonth;
        set => SetFormProperty(ref _selectedMonth, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetFormProperty(ref _isActive, value);
    }

    public int SelectedMonthNumber => SelectedMonth?.Month ?? 0;

    public int SelectedYear => SelectedMonth?.Year ?? 0;

    protected override string EditParameterKey => NavigationParameterKeys.BudgetId;

    protected override string? CancelNavigationFallbackRoute => AppRoutes.BudgetsOverview;

    protected override void ApplyNavigationParameters(Dictionary<string, object>? parameters)
        => _saveNavigationRoute = ResolveSaveNavigationRoute(parameters);

    protected override Task InitializeForCreateAsync()
    {
        Title = "Nouveau budget";
        AmountText = string.Empty;
        SelectedMonth = MonthOptions.FirstOrDefault();
        IsActive = true;
        _createdAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    protected override async Task LoadForEditAsync(int entityId)
    {
        var result = await _budgetService.GetBudgetByIdAsync(entityId, CurrentUserId);
        if (!result.IsSuccess || result.Data == null)
        {
            ErrorMessage = result.Message;
            return;
        }

        Budget budget = result.Data;
        Title = "Modifier le budget";
        AmountText = budget.Amount.ToString("0.##");
        SelectedMonth = MonthOptions.FirstOrDefault(option => option.Year == budget.StartDate.Year && option.Month == budget.StartDate.Month);
        IsActive = budget.IsActive;
        _createdAt = budget.CreatedAt;
    }

    protected override string ValidateForm()
    {
        if (CurrentUserId <= 0)
            return "Aucune session utilisateur active.";

        if (!TryParseDecimalInput(AmountText, out decimal amount) || amount <= 0)
            return "Le montant du budget doit être strictement positif.";

        if (SelectedMonth == null)
            return "Le mois du budget est requis.";

        if (SelectedMonth.StartDate.Date > DateTime.Today)
            return "Impossible de créer un budget pour un mois futur.";

        return string.Empty;
    }

    protected override async Task<bool> SaveCoreAsync()
    {
        _ = TryParseDecimalInput(AmountText, out decimal amount);

        if (SelectedMonth == null)
            return false;

        Budget budget = new()
        {
            Id = EditingEntityId,
            UserId = CurrentUserId,
            Amount = amount,
            PeriodType = "Monthly",
            StartDate = SelectedMonth.StartDate,
            EndDate = SelectedMonth.EndDate,
            IsActive = IsActive,
            CreatedAt = _createdAt
        };

        bool isUpdatingExistingBudget = EditingEntityId > 0;

        var result = isUpdatingExistingBudget
            ? await _budgetService.UpdateBudgetAsync(budget)
            : await _budgetService.CreateBudgetAsync(budget);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        _appEventBus.PublishDataChanged(AppDataChangeKind.Budgets);
        await NavigationService.NavigateToAsync(_saveNavigationRoute);
        return true;
    }

    protected override async Task<bool> DeleteCoreAsync()
    {
        if (!IsEditMode)
            return false;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Suppression",
            "Supprimer ce budget mensuel ?",
            "Oui",
            "Non");

        if (!confirm)
            return false;

        var result = await _budgetService.DeleteBudgetAsync(EditingEntityId, CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        _appEventBus.PublishDataChanged(AppDataChangeKind.Budgets);
        await NavigationService.NavigateToAsync(AppRoutes.BudgetsOverview);
        return true;
    }

    private static string ResolveSaveNavigationRoute(Dictionary<string, object>? parameters)
    {
        if (parameters?.TryGetValue(NavigationParameterKeys.ReturnRoute, out object? value) != true)
            return AppRoutes.BudgetsOverview;

        string? route = value as string;
        return string.Equals(route, AppRoutes.Dashboard, StringComparison.Ordinal)
            ? AppRoutes.Dashboard
            : AppRoutes.BudgetsOverview;
    }

    private static IReadOnlyList<MonthOptionViewModel> BuildMonthOptions()
    {
        DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

        return Enumerable.Range(0, 12)
            .Select(offset => currentMonth.AddMonths(-offset))
            .Select(date => new MonthOptionViewModel(date))
            .ToList();
    }
}

public sealed class MonthOptionViewModel
{
    public MonthOptionViewModel(DateTime monthStart)
    {
        StartDate = new DateTime(monthStart.Year, monthStart.Month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);
    }

    public int Year => StartDate.Year;

    public int Month => StartDate.Month;

    public DateTime StartDate { get; }

    public DateTime EndDate { get; }

    public string Label => StartDate.ToString("MMMM yyyy");
}
