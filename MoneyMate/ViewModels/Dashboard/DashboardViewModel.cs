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

public class DashboardViewModel : AuthenticatedViewModelBase, IAuthenticatedHeaderViewModel
{
    private static readonly Color DefaultColor = Color.FromArgb("#6793AE");
    private static readonly Color SuccessColor = Color.FromArgb("#6CC57C");
    private static readonly Color DangerColor = Color.FromArgb("#E57373");
    private static readonly Color AccentColor = Color.FromArgb("#26658C");
    private const AppDataChangeKind RefreshChangeKinds = AppDataChangeKind.All;

    private readonly IDashboardService _dashboardService;
    private readonly IAppEventBus _appEventBus;

    private long _lastRefreshVersion = -1;
    private string _greetingText = "Bonjour";
    private string _todayDisplay = string.Empty;
    private string _userName = string.Empty;
    private string _devise = "EUR";
    private decimal _currentBalance;
    private decimal _totalIncome;
    private decimal _totalExpenses;
    private int _triggeredAlertsCount;
    private string _balanceDisplay = CurrencyHelper.FormatSigned(0m);
    private string _totalIncomeDisplay = CurrencyHelper.Format(0m);
    private string _totalExpensesDisplay = CurrencyHelper.Format(0m);
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
    private string _balanceTrendDisplay = "+0 %";
    private string _incomeTrendDisplay = "Budget du mois";
    private string _expensesTrendDisplay = "0 % vs mois dernier";
    private string _insightMessage = "Ajoutez quelques dépenses pour obtenir un conseil personnalisé.";
    private string _lastUpdatedDisplay = string.Empty;
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
        Budgets = [];
        TopCategories = [];
        RecentTransactions = [];
        TopCategorySegments = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        NavigateToExpensesCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.ExpensesList));
        NavigateToAddExpenseCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AddExpense));
        NavigateToAlertsCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AlertThreshold));
        NavigateToProfileCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.Profile));
        NavigateToCalendarCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.Calendar));
        NavigateToAddIncomeCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AddExpense));
        NavigateToTransferCommand = new Command(async () => await ShowUpcomingFeatureAsync("Transfert"));
        NavigateToScannerCommand = new Command(async () => await ShowUpcomingFeatureAsync("Scanner de ticket"));
        NavigateToPremiumCommand = new Command(async () => await ShowUpcomingFeatureAsync("MoneyMate Premium"));
        OpenCategoriesCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.CategoriesList));
        OpenFixedChargesCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.FixedCharges));
        OpenAlertsCommand = NavigateToAlertsCommand;
        CreateBudgetCommand = new Command(async () => await OpenAddBudgetAsync());

        UpdateCurrentUserContext();
        ApplySummary(new DashboardSummary());
    }

    public ObservableCollection<CategoryBudget> Budgets { get; }

    public ObservableCollection<DashboardCategoryItemViewModel> TopCategories { get; }

    public ObservableCollection<RecentTransactionItemViewModel> RecentTransactions { get; }

    public ObservableCollection<DonutChartSegment> TopCategorySegments { get; }

    public ICommand RefreshCommand { get; }

    public ICommand LogoutCommand { get; }

    public ICommand NavigateToExpensesCommand { get; }

    public ICommand NavigateToAddExpenseCommand { get; }

    public ICommand NavigateToAlertsCommand { get; }

    public ICommand NavigateToProfileCommand { get; }

    public ICommand NavigateToCalendarCommand { get; }

    public ICommand NavigateToAddIncomeCommand { get; }

    public ICommand NavigateToTransferCommand { get; }

    public ICommand NavigateToScannerCommand { get; }

    public ICommand NavigateToPremiumCommand { get; }

    public ICommand OpenCategoriesCommand { get; }

    public ICommand OpenFixedChargesCommand { get; }

    public ICommand OpenAlertsCommand { get; }

    public ICommand CreateBudgetCommand { get; }

    public decimal CurrentBalance
    {
        get => _currentBalance;
        private set
        {
            if (SetProperty(ref _currentBalance, value))
                OnPropertyChanged(nameof(BalanceColor));
        }
    }

    public decimal TotalIncome
    {
        get => _totalIncome;
        private set => SetProperty(ref _totalIncome, value);
    }

    public decimal TotalExpenses
    {
        get => _totalExpenses;
        private set => SetProperty(ref _totalExpenses, value);
    }

    public int TriggeredAlertsCount
    {
        get => _triggeredAlertsCount;
        private set
        {
            if (SetProperty(ref _triggeredAlertsCount, value))
            {
                OnPropertyChanged(nameof(HasTriggeredAlerts));
                OnPropertyChanged(nameof(HasNotificationBadge));
            }
        }
    }

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

    public string BalanceDisplay
    {
        get => _balanceDisplay;
        private set => SetProperty(ref _balanceDisplay, value);
    }

    public string TotalIncomeDisplay
    {
        get => _totalIncomeDisplay;
        private set => SetProperty(ref _totalIncomeDisplay, value);
    }

    public string TotalExpensesDisplay
    {
        get => _totalExpensesDisplay;
        private set => SetProperty(ref _totalExpensesDisplay, value);
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

    public string BalanceTrendDisplay
    {
        get => _balanceTrendDisplay;
        private set => SetProperty(ref _balanceTrendDisplay, value);
    }

    public string IncomeTrendDisplay
    {
        get => _incomeTrendDisplay;
        private set => SetProperty(ref _incomeTrendDisplay, value);
    }

    public string ExpensesTrendDisplay
    {
        get => _expensesTrendDisplay;
        private set => SetProperty(ref _expensesTrendDisplay, value);
    }

    public string InsightMessage
    {
        get => _insightMessage;
        private set => SetProperty(ref _insightMessage, value);
    }

    public string LastUpdatedDisplay
    {
        get => _lastUpdatedDisplay;
        private set => SetProperty(ref _lastUpdatedDisplay, value);
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

    public Color BalanceColor => CurrentBalance >= 0 ? SuccessColor : DangerColor;

    public Color IncomeColor => SuccessColor;

    public Color ExpensesColor => DangerColor;

    public bool HasBudgets => Budgets.Count > 0;

    public bool HasTopCategories => TopCategories.Count > 0;

    public bool HasRecentTransactions => RecentTransactions.Count > 0;

    public bool HasTriggeredAlerts => TriggeredAlertsCount > 0;

    public bool HasNotificationBadge => HasTriggeredAlerts;

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

    private async Task ShowUpcomingFeatureAsync(string featureName)
        => await DialogService.ShowAlertAsync(featureName, "Cette fonctionnalité est prête côté navigation et sera connectée au module premium.", "OK");

    private void ApplySummary(DashboardSummary summary, bool showCurrentMonthBudgetEmptyState = false)
    {
        CurrentBalance = summary.CurrentMonthBalance;
        TotalIncome = summary.CurrentMonthBudget;
        TotalExpenses = summary.CurrentMonthExpenses;
        TriggeredAlertsCount = summary.TriggeredAlertsCount;

        BalanceDisplay = CurrencyHelper.FormatSigned(CurrentBalance, Devise);
        TotalIncomeDisplay = CurrencyHelper.Format(TotalIncome, Devise);
        TotalExpensesDisplay = CurrencyHelper.Format(TotalExpenses, Devise);
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
        BalanceTrendDisplay = BuildBalanceTrend(summary);
        IncomeTrendDisplay = summary.HasCurrentMonthBudget ? "Budget actif" : "À configurer";
        ExpensesTrendDisplay = BuildExpensesTrend(summary);
        InsightMessage = BuildInsightMessage(summary);
        LastUpdatedDisplay = $"Mis à jour {DateTime.Now:HH:mm}";
        HasCurrentMonthBudget = summary.HasCurrentMonthBudget;
        IsCurrentMonthBudgetMissing = showCurrentMonthBudgetEmptyState && !summary.HasCurrentMonthBudget;

        UpdateBudgets(summary.Budgets ?? []);
        UpdateTopCategories(summary.TopCategories ?? []);
        UpdateRecentTransactions(summary.RecentTransactions ?? []);
    }

    private void UpdateCurrentUserContext()
    {
        Devise = CurrentDevise;
        UserName = BuildUserName(CurrentUser?.Email);
        GreetingText = string.IsNullOrWhiteSpace(UserName) ? "Bonjour 👋" : $"Bonjour {UserName} 👋";
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

    private void UpdateBudgets(IEnumerable<DashboardBudgetProgress> budgets)
    {
        Budgets.Clear();

        foreach (DashboardBudgetProgress budget in budgets)
        {
            Color progressColor = budget.IsExceeded
                ? DangerColor
                : budget.ConsumedPercentage >= 80m
                    ? AccentColor
                    : SuccessColor;

            Budgets.Add(new CategoryBudget
            {
                BudgetId = budget.BudgetId,
                CategoryId = budget.CategoryId,
                CategoryName = budget.CategoryName,
                Icon = string.IsNullOrWhiteSpace(budget.CategoryIcon) ? "💰" : budget.CategoryIcon,
                SpentAmount = budget.SpentAmount,
                BudgetAmount = budget.BudgetAmount,
                RemainingAmount = budget.RemainingAmount,
                Progress = (double)Math.Clamp(budget.ConsumedPercentage / 100m, 0m, 1m),
                AmountDisplay = $"{CurrencyHelper.Format(budget.SpentAmount, Devise)} / {CurrencyHelper.Format(budget.BudgetAmount, Devise)}",
                RemainingDisplay = budget.RemainingAmount >= 0
                    ? $"{CurrencyHelper.Format(budget.RemainingAmount, Devise)} restants"
                    : $"{CurrencyHelper.Format(Math.Abs(budget.RemainingAmount), Devise)} dépassés",
                PercentageDisplay = $"{Math.Round(budget.ConsumedPercentage, 0):0} %",
                AccentColor = TryCreateColor(budget.CategoryColor),
                ProgressColor = progressColor
            });
        }

        OnPropertyChanged(nameof(HasBudgets));
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

        foreach (DashboardRecentTransaction transaction in transactions.Take(5))
        {
            RecentTransactions.Add(new RecentTransactionItemViewModel
            {
                ExpenseId = transaction.ExpenseId,
                Title = string.IsNullOrWhiteSpace(transaction.Note)
                    ? transaction.CategoryName
                    : transaction.Note,
                Subtitle = transaction.CategoryName,
                AmountDisplay = $"-{CurrencyHelper.Format(transaction.Amount, Devise)}",
                AmountColor = DangerColor,
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

    private static string BuildBalanceTrend(DashboardSummary summary)
    {
        if (summary.CurrentMonthBudget <= 0)
            return "Budget à créer";

        decimal ratio = Math.Round(summary.CurrentMonthBalance / summary.CurrentMonthBudget * 100m, 0);
        return $"{ratio:+0;-0;0} % disponible";
    }

    private static string BuildExpensesTrend(DashboardSummary summary)
    {
        if (summary.PreviousMonthExpenses <= 0)
            return "Nouveau mois";

        decimal variation = Math.Round(summary.ExpensesDeltaFromPreviousMonth / summary.PreviousMonthExpenses * 100m, 0);
        return $"{variation:+0;-0;0} % vs mois dernier";
    }

    private static string BuildInsightMessage(DashboardSummary summary)
    {
        if (summary.CurrentMonthExpensesCount == 0)
            return "Aucune dépense ce mois-ci : votre budget démarre avec une belle marge.";

        if (summary.BudgetsAtRiskCount > 0)
            return $"{summary.BudgetsAtRiskCount} budget(s) approchent du seuil : jetez un oeil aux catégories à risque.";

        if (summary.PreviousMonthExpenses > 0)
        {
            decimal variation = Math.Round(summary.ExpensesDeltaFromPreviousMonth / summary.PreviousMonthExpenses * 100m, 0);
            if (variation < 0)
                return $"Vous dépensez {Math.Abs(variation):0} % de moins que le mois dernier. Beau rythme.";

            if (variation > 0)
                return $"Vos dépenses montent de {variation:0} % : surveillez les postes récurrents.";
        }

        if (summary.CurrentMonthBalance >= 0)
            return "Votre solde mensuel reste positif : continuez à piloter vos enveloppes.";

        return "Votre solde mensuel est négatif : priorisez les dépenses essentielles cette semaine.";
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
