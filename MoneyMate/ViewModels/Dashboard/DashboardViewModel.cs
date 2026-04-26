using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MoneyMate.Components;
using MoneyMate.Configuration;
using MoneyMate.Helpers;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;

namespace MoneyMate.ViewModels.Dashboard;

public class DashboardViewModel : AuthenticatedViewModelBase
{
    private static readonly Color DefaultColor = Color.FromArgb("#6B7A8F");
    private const AppDataChangeKind RefreshChangeKinds = AppDataChangeKind.All;

    private readonly IDashboardService _dashboardService;
    private readonly IAppEventBus _appEventBus;

    private long _lastRefreshVersion = -1;
    private string _greetingText = "Bonjour";
    private string _todayDisplay = string.Empty;
    private string _userName = string.Empty;
    private string _devise = "EUR";
    private string _currentMonthExpensesDisplay = CurrencyHelper.Format(0m);
    private string _expensesCountDisplay = "0";
    private string _activeBudgetsDisplay = "0";
    private string _activeFixedChargesDisplay = "0";
    private string _activeAlertsDisplay = "0";
    private string _triggeredAlertsDisplay = "0";
    private string _budgetsAtRiskDisplay = "0";
    private string _previousMonthDeltaDisplay = CurrencyHelper.FormatSigned(0m);
    private string _currentMonthBudgetDisplay = CurrencyHelper.Format(0m);
    private string _currentMonthBalanceDisplay = CurrencyHelper.FormatSigned(0m);
    private bool _hasCurrentMonthBudget;
    private bool _isCurrentMonthBudgetMissing;

    public DashboardViewModel(
        IAuthenticationService authenticationService,
        IDashboardService dashboardService,
        IDialogService dialogService,
        INavigationService navigationService,
        IAppEventBus? appEventBus = null)
        : base(authenticationService, dialogService, navigationService)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _appEventBus = appEventBus ?? NullAppEventBus.Instance;

        Title = "Tableau de bord";
        TopCategories = [];
        RecentTransactions = [];
        TopCategorySegments = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        OpenCategoriesCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.CategoriesList));
        OpenFixedChargesCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.FixedCharges));
        OpenAlertsCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AlertThreshold));
        CreateBudgetCommand = new Command(async () => await OpenAddBudgetAsync());

        UpdateCurrentUserContext();
        ApplySummary(new DashboardSummary());
    }

    public ObservableCollection<DashboardCategoryItemViewModel> TopCategories { get; }

    public ObservableCollection<RecentTransactionItemViewModel> RecentTransactions { get; }

    public ObservableCollection<DonutChartSegment> TopCategorySegments { get; }

    public ICommand RefreshCommand { get; }

    public ICommand LogoutCommand { get; }

    public ICommand OpenCategoriesCommand { get; }

    public ICommand OpenFixedChargesCommand { get; }

    public ICommand OpenAlertsCommand { get; }

    public ICommand CreateBudgetCommand { get; }

    public string UserName
    {
        get => _userName;
        private set => SetProperty(ref _userName, value);
    }

    public string GreetingText
    {
        get => _greetingText;
        private set => SetProperty(ref _greetingText, value);
    }

    public string TodayDisplay
    {
        get => _todayDisplay;
        private set => SetProperty(ref _todayDisplay, value);
    }

    public string Devise
    {
        get => _devise;
        private set => SetProperty(ref _devise, value);
    }

    public string CurrentMonthExpensesDisplay
    {
        get => _currentMonthExpensesDisplay;
        private set => SetProperty(ref _currentMonthExpensesDisplay, value);
    }

    public string ExpensesCountDisplay
    {
        get => _expensesCountDisplay;
        private set => SetProperty(ref _expensesCountDisplay, value);
    }

    public string ActiveBudgetsDisplay
    {
        get => _activeBudgetsDisplay;
        private set => SetProperty(ref _activeBudgetsDisplay, value);
    }

    public string ActiveFixedChargesDisplay
    {
        get => _activeFixedChargesDisplay;
        private set => SetProperty(ref _activeFixedChargesDisplay, value);
    }

    public string ActiveAlertsDisplay
    {
        get => _activeAlertsDisplay;
        private set => SetProperty(ref _activeAlertsDisplay, value);
    }

    public string TriggeredAlertsDisplay
    {
        get => _triggeredAlertsDisplay;
        private set => SetProperty(ref _triggeredAlertsDisplay, value);
    }

    public string BudgetsAtRiskDisplay
    {
        get => _budgetsAtRiskDisplay;
        private set => SetProperty(ref _budgetsAtRiskDisplay, value);
    }

    public string PreviousMonthDeltaDisplay
    {
        get => _previousMonthDeltaDisplay;
        private set => SetProperty(ref _previousMonthDeltaDisplay, value);
    }

    public string CurrentMonthBudgetDisplay
    {
        get => _currentMonthBudgetDisplay;
        private set => SetProperty(ref _currentMonthBudgetDisplay, value);
    }

    public string CurrentMonthBalanceDisplay
    {
        get => _currentMonthBalanceDisplay;
        private set => SetProperty(ref _currentMonthBalanceDisplay, value);
    }

    public bool HasCurrentMonthBudget
    {
        get => _hasCurrentMonthBudget;
        private set => SetProperty(ref _hasCurrentMonthBudget, value);
    }

    public bool IsCurrentMonthBudgetMissing
    {
        get => _isCurrentMonthBudgetMissing;
        private set => SetProperty(ref _isCurrentMonthBudgetMissing, value);
    }

    public bool HasTopCategories => TopCategories.Count > 0;

    public bool HasRecentTransactions => RecentTransactions.Count > 0;

    public Task InitializeAsync()
        => RefreshIfNeededAsync();

    public async Task RefreshIfNeededAsync()
    {
        if (_lastRefreshVersion < 0 || _appEventBus.HasChangedSince(RefreshChangeKinds, _lastRefreshVersion))
            await LoadAsync();
    }

    public async Task LoadAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            UpdateCurrentUserContext();

            if (!EnsureCurrentUser())
            {
                ApplySummary(new DashboardSummary());
                return;
            }

            var summaryResult = await _dashboardService.GetDashboardSummaryAsync(CurrentUserId);
            if (!summaryResult.IsSuccess || summaryResult.Data is null)
            {
                ApplySummary(new DashboardSummary());
                ErrorMessage = summaryResult.Message;
                return;
            }

            ApplySummary(summaryResult.Data, showCurrentMonthBudgetEmptyState: true);
            UpdateObservedRefreshVersion();
        }, "Une erreur est survenue lors du chargement du tableau de bord.");
    }

    private void UpdateObservedRefreshVersion()
        => _lastRefreshVersion = _appEventBus.GetVersion(RefreshChangeKinds);

    private async Task LogoutAsync()
    {
        if (IsBusy)
            return;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Déconnexion",
            "Voulez-vous vous déconnecter ?",
            "Oui",
            "Non");

        if (!confirm)
            return;

        await AuthenticationService.LogoutAsync(true);
        await NavigationService.NavigateToAsync(AppRoutes.Main);
    }

    private async Task OpenAddBudgetAsync()
    {
        await NavigationService.NavigateToAsync(
            AppRoutes.AddBudget,
            new Dictionary<string, object>
            {
                [NavigationParameterKeys.ReturnRoute] = AppRoutes.Dashboard
            });
    }

    private void ApplySummary(DashboardSummary summary, bool showCurrentMonthBudgetEmptyState = false)
    {
        CurrentMonthExpensesDisplay = CurrencyHelper.Format(summary.CurrentMonthExpenses, Devise);
        ExpensesCountDisplay = summary.CurrentMonthExpensesCount.ToString();
        ActiveBudgetsDisplay = summary.ActiveBudgetsCount.ToString();
        ActiveFixedChargesDisplay = summary.ActiveFixedChargesCount.ToString();
        ActiveAlertsDisplay = summary.ActiveAlertsCount.ToString();
        TriggeredAlertsDisplay = summary.TriggeredAlertsCount.ToString();
        BudgetsAtRiskDisplay = summary.BudgetsAtRiskCount.ToString();
        PreviousMonthDeltaDisplay = CurrencyHelper.FormatSigned(summary.ExpensesDeltaFromPreviousMonth, Devise);
        CurrentMonthBudgetDisplay = CurrencyHelper.Format(summary.CurrentMonthBudget, Devise);
        CurrentMonthBalanceDisplay = CurrencyHelper.FormatSigned(summary.CurrentMonthBalance, Devise);
        HasCurrentMonthBudget = summary.HasCurrentMonthBudget;
        IsCurrentMonthBudgetMissing = showCurrentMonthBudgetEmptyState && !summary.HasCurrentMonthBudget;

        UpdateTopCategories(summary.TopCategories ?? []);
        UpdateRecentTransactions(summary.RecentTransactions ?? []);
    }

    private void UpdateCurrentUserContext()
    {
        Devise = CurrentDevise;
        UserName = BuildUserName(CurrentUser?.Email);
        GreetingText = string.IsNullOrWhiteSpace(UserName) ? "Bonjour" : $"Bonjour {UserName}";
        TodayDisplay = FormatToday(DateTime.Today);
    }

    private static string BuildUserName(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        string[] parts = email.Split('@', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? string.Empty : parts[0];
    }

    private static string FormatToday(DateTime date)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("fr-FR");
        string text = date.ToString("dddd d MMMM yyyy", culture);
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : char.ToUpper(text[0], culture) + text[1..];
    }

    private void UpdateTopCategories(IEnumerable<DashboardCategorySpending> categories)
    {
        List<DashboardCategorySpending> materializedCategories = categories.ToList();
        decimal totalAmount = materializedCategories.Sum(category => category.TotalAmount);

        TopCategories.Clear();
        TopCategorySegments.Clear();

        foreach (DashboardCategorySpending category in materializedCategories)
        {
            Color color = TryCreateColor(category.CategoryColor);
            DonutChartSegment segment = new()
            {
                Value = (double)category.TotalAmount,
                Color = color
            };

            TopCategorySegments.Add(segment);
            TopCategories.Add(new DashboardCategoryItemViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                TotalAmount = category.TotalAmount,
                ExpensesCount = category.ExpensesCount,
                AmountDisplay = CurrencyHelper.Format(category.TotalAmount, Devise),
                PercentageDisplay = BuildPercentageDisplay(category.TotalAmount, totalAmount),
                ExpensesCountText = category.ExpensesCount <= 1
                    ? "1 opération"
                    : $"{category.ExpensesCount} opérations",
                SegmentColor = color,
                ChartSegment = segment
            });
        }

        OnPropertyChanged(nameof(HasTopCategories));
    }

    private void UpdateRecentTransactions(IEnumerable<DashboardRecentTransaction> transactions)
    {
        RecentTransactions.Clear();

        foreach (DashboardRecentTransaction transaction in transactions)
        {
            RecentTransactions.Add(new RecentTransactionItemViewModel
            {
                ExpenseId = transaction.ExpenseId,
                Title = string.IsNullOrWhiteSpace(transaction.Note)
                    ? transaction.CategoryName
                    : transaction.Note,
                Subtitle = transaction.CategoryName,
                AmountDisplay = CurrencyHelper.Format(transaction.Amount, Devise),
                DateDisplay = transaction.DateOperation.ToString("dd/MM/yyyy"),
                AccentColor = TryCreateColor(transaction.CategoryColor),
                Icon = string.IsNullOrWhiteSpace(transaction.CategoryIcon)
                    ? "💰"
                    : transaction.CategoryIcon
            });
        }

        OnPropertyChanged(nameof(HasRecentTransactions));
    }

    private static string BuildPercentageDisplay(decimal amount, decimal totalAmount)
    {
        if (amount <= 0 || totalAmount <= 0)
            return "0 %";

        decimal percentage = Math.Round(amount / totalAmount * 100m, 1);
        return $"{percentage:0.#} %";
    }

    private static Color TryCreateColor(string? colorValue)
    {
        if (!string.IsNullOrWhiteSpace(colorValue))
        {
            try
            {
                return Color.FromArgb(colorValue);
            }
            catch
            {
            }
        }

        return DefaultColor;
    }
}
