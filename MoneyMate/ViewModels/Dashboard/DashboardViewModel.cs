using System.Collections.ObjectModel;
using System.Windows.Input;
using MoneyMate.Helpers;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;

namespace MoneyMate.ViewModels.Dashboard
{
    /// <summary>
    /// ViewModel du tableau de bord alimenté par le service métier.
    /// </summary>
    public class DashboardViewModel : AuthenticatedViewModelBase
    {
        private readonly IDashboardService _dashboardService;
        private string _userName = string.Empty;
        private string _currentMonthExpensesDisplay = "0,00 €";
        private string _expensesCountDisplay = "0";
        private string _activeBudgetsDisplay = "0";
        private string _activeFixedChargesDisplay = "0";
        private string _activeAlertsDisplay = "0";
        private string _triggeredAlertsDisplay = "0";
        private string _budgetsAtRiskDisplay = "0";
        private string _previousMonthDeltaDisplay = "0,00 €";

        public DashboardViewModel(
            IAuthenticationService authenticationService,
            IDashboardService dashboardService,
            IDialogService dialogService,
            INavigationService navigationService)
            : base(authenticationService, dialogService, navigationService)
        {
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
            Title = "Tableau de Bord";

            TopCategories = new ObservableCollection<DashboardTopCategoryItemViewModel>();

            LogoutCommand = new Command(async () => await LogoutAsync());
            RefreshCommand = new Command(async () => await LoadAsync());
            GoHomeCommand = new Command(async () => await NavigationService.NavigateToAsync("//DashboardPage"));
            GoExpensesCommand = new Command(async () => await NavigationService.NavigateToAsync("//ExpensesListPage"));
            GoBudgetCommand = new Command(async () => await NavigationService.NavigateToAsync("//BudgetsOverviewPage"));
            GoProfileCommand = new Command(async () => await NavigationService.NavigateToAsync("//ProfilePage"));
        }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
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

        public bool HasTopCategories => TopCategories.Count > 0;

        public ObservableCollection<DashboardTopCategoryItemViewModel> TopCategories { get; }

        public ICommand LogoutCommand { get; }

        public ICommand RefreshCommand { get; }

        public ICommand GoHomeCommand { get; }

        public ICommand GoExpensesCommand { get; }

        public ICommand GoBudgetCommand { get; }

        public ICommand GoProfileCommand { get; }

        public async Task LoadAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                if (!EnsureCurrentUser())
                    return;

                UserName = CurrentUser?.Email ?? "Utilisateur";

                var summaryResult = await _dashboardService.GetDashboardSummaryAsync(CurrentUserId);
                if (!summaryResult.IsSuccess || summaryResult.Data == null)
                {
                    ErrorMessage = summaryResult.Message;
                    TopCategories.Clear();
                    OnPropertyChanged(nameof(HasTopCategories));
                    return;
                }

                ApplySummary(summaryResult.Data);
            }, "Une erreur est survenue lors du chargement du tableau de bord.");
        }

        private void ApplySummary(DashboardSummary summary)
        {
            string devise = CurrentUser?.Devise ?? "EUR";

            CurrentMonthExpensesDisplay = CurrencyHelper.Format(summary.CurrentMonthExpenses, devise);
            ExpensesCountDisplay = summary.CurrentMonthExpensesCount.ToString();
            ActiveBudgetsDisplay = summary.ActiveBudgetsCount.ToString();
            ActiveFixedChargesDisplay = summary.ActiveFixedChargesCount.ToString();
            ActiveAlertsDisplay = summary.ActiveAlertsCount.ToString();
            TriggeredAlertsDisplay = summary.TriggeredAlertsCount.ToString();
            BudgetsAtRiskDisplay = summary.BudgetsAtRiskCount.ToString();
            PreviousMonthDeltaDisplay = CurrencyHelper.FormatSigned(summary.ExpensesDeltaFromPreviousMonth, devise);

            TopCategories.Clear();
            foreach (DashboardCategorySpending category in summary.TopCategories.OrderByDescending(item => item.TotalAmount))
            {
                TopCategories.Add(new DashboardTopCategoryItemViewModel
                {
                    CategoryName = category.CategoryName,
                    TotalAmount = category.TotalAmount,
                    ExpensesCount = category.ExpensesCount,
                    Devise = devise
                });
            }

            OnPropertyChanged(nameof(HasTopCategories));
        }

        private async Task LogoutAsync()
        {
            bool confirm = await DialogService.ShowConfirmationAsync(
                "Déconnexion",
                "Voulez-vous vraiment vous déconnecter ?",
                "Oui",
                "Non");

            if (!confirm)
                return;

            await AuthenticationService.LogoutAsync();
            await NavigationService.NavigateToAsync("//MainPage");
        }
    }

    public sealed class DashboardTopCategoryItemViewModel
    {
        public string CategoryName { get; init; } = string.Empty;

        public decimal TotalAmount { get; init; }

        public int ExpensesCount { get; init; }

        public string Devise { get; init; } = "EUR";

        public string ExpensesCountText => $"{ExpensesCount} dépense(s)";
    }
}
