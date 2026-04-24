using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;

namespace MoneyMate.ViewModels.Budgets;

/// <summary>
/// ViewModel de synthèse des budgets mensuels globaux.
/// </summary>
public class BudgetsOverviewViewModel : AuthenticatedViewModelBase
{
    private readonly IBudgetService _budgetService;
    private decimal _totalBudgetAmount;
    private decimal _totalConsumedAmount;

    public BudgetsOverviewViewModel(
        IBudgetService budgetService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));

        Title = "Budgets";
        Budgets = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        AddBudgetCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AddBudget));
        ManageCategoriesCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.CategoriesList));
        OpenEditBudgetCommand = new Command<BudgetOverviewItemViewModel>(async budget => await OpenEditBudgetAsync(budget));
    }

    public ObservableCollection<BudgetOverviewItemViewModel> Budgets { get; }

    public ICommand RefreshCommand { get; }

    public ICommand AddBudgetCommand { get; }

    public ICommand ManageCategoriesCommand { get; }

    public ICommand OpenEditBudgetCommand { get; }

    public decimal TotalBudgetAmount
    {
        get => _totalBudgetAmount;
        private set => SetProperty(ref _totalBudgetAmount, value);
    }

    public decimal TotalConsumedAmount
    {
        get => _totalConsumedAmount;
        private set => SetProperty(ref _totalConsumedAmount, value);
    }

    public int ActiveBudgetsCount => Budgets.Count;

    public int BudgetsAtRiskCount => Budgets.Count(budget => budget.IsAtRisk);

    public bool HasBudgets => Budgets.Count > 0;

    public string Devise => CurrentDevise;

    public async Task LoadAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            var budgetsResult = await _budgetService.GetBudgetsAsync(CurrentUserId);
            if (!budgetsResult.IsSuccess)
            {
                ErrorMessage = budgetsResult.Message;
                RefreshState();
                return;
            }

            List<BudgetOverviewItemViewModel> budgetItems = [];
            foreach (Budget budget in (budgetsResult.Data ?? []).OrderByDescending(budget => budget.StartDate))
            {
                var summaryResult = await _budgetService.GetBudgetConsumptionSummaryAsync(budget.Id, CurrentUserId);
                if (!summaryResult.IsSuccess || summaryResult.Data == null)
                    continue;

                budgetItems.Add(BudgetOverviewItemViewModel.FromData(budget, summaryResult.Data, Devise));
            }

            Budgets.Clear();
            foreach (BudgetOverviewItemViewModel budgetItem in budgetItems)
                Budgets.Add(budgetItem);

            TotalBudgetAmount = budgetItems.Sum(item => item.BudgetAmount);
            TotalConsumedAmount = budgetItems.Sum(item => item.ConsumedAmount);
            RefreshState();
        }, "Une erreur est survenue lors du chargement des budgets.");
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(ActiveBudgetsCount));
        OnPropertyChanged(nameof(BudgetsAtRiskCount));
        OnPropertyChanged(nameof(HasBudgets));
        OnPropertyChanged(nameof(Devise));
    }

    private async Task OpenEditBudgetAsync(BudgetOverviewItemViewModel? budget)
    {
        if (budget == null)
            return;

        await NavigationService.NavigateToAsync($"{AppRoutes.EditBudget}?{NavigationParameterKeys.BudgetId}={budget.Id}");
    }
}

public sealed class BudgetOverviewItemViewModel
{
    private static readonly Color DefaultColor = Color.FromArgb("#6B7A8F");

    public int Id { get; init; }

    public string PeriodLabel { get; init; } = string.Empty;

    public string CategoryName => PeriodLabel;

    public string SummaryLabel { get; init; } = string.Empty;

    public Color AccentColor { get; init; } = DefaultColor;

    public decimal BudgetAmount { get; init; }

    public decimal ConsumedAmount { get; init; }

    public decimal RemainingAmount { get; init; }

    public decimal ConsumedPercentage { get; init; }

    public string Devise { get; init; } = "EUR";

    public bool IsExceeded { get; init; }

    public bool IsAtRisk => ConsumedPercentage >= 80m;

    public string StatusText => IsExceeded
        ? "Dépassé"
        : IsAtRisk
            ? "À surveiller"
            : "Maîtrisé";

    public static BudgetOverviewItemViewModel FromData(
        Budget budget,
        BudgetConsumptionSummary summary,
        string devise)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentNullException.ThrowIfNull(summary);

        return new BudgetOverviewItemViewModel
        {
            Id = budget.Id,
            PeriodLabel = budget.StartDate.ToString("MMMM yyyy"),
            SummaryLabel = BuildSummaryLabel(budget),
            AccentColor = DefaultColor,
            BudgetAmount = summary.Budget?.Amount ?? budget.Amount,
            ConsumedAmount = summary.ConsumedAmount,
            RemainingAmount = summary.RemainingAmount,
            ConsumedPercentage = summary.ConsumedPercentage,
            Devise = devise,
            IsExceeded = summary.IsExceeded
        };
    }

    private static string BuildSummaryLabel(Budget budget)
        => $"Budget global • {budget.StartDate:MM/yyyy}";
}
