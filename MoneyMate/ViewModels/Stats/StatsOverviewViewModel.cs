using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Microcharts;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using SkiaSharp;

namespace MoneyMate.ViewModels.Stats;

public sealed class StatsOverviewViewModel : AuthenticatedViewModelBase
{
    private const int MaxVisibleCategories = 5;
    private const string PrimaryColor = "#6B7A8F";
    private const string SuccessColor = "#5CB85C";
    private const string DangerColor = "#D9534F";
    private const string OtherCategoryName = "Autres";

    private readonly IExpenseService _expenseService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;

    private string _periodDisplay = string.Empty;
    private string _netBalanceDisplay = CurrencyHelper.FormatSigned(0m);
    private string _expensesDisplay = CurrencyHelper.Format(0m);
    private string _expensesCountDisplay = "0 opération";
    private string _incomeHintDisplay = "Budget mensuel non défini";
    private string _lastUpdatedDisplay = string.Empty;
    private bool _hasCategoryChart;
    private bool _hasMonthlyBarChart;
    private bool _hasEvolutionChart;
    private string _selectedEvolutionPeriod = "Année";
    private Chart _categoryPieChart = CreateEmptyPieChart();
    private Chart _currentMonthBarChart = CreateEmptyBarChart();
    private Chart _evolutionLineChart = CreateEmptyLineChart();
    private MonthlyStatsDto _monthlyStats = new();
    private bool _isPremiumUser;

    public StatsOverviewViewModel(
        IAuthenticationService authenticationService,
        IExpenseService expenseService,
        IBudgetService budgetService,
        ICategoryService categoryService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        Title = "Statistiques";
        IsPremiumUser = false;
        CategoryStats = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        SelectEvolutionPeriodCommand = new Command<string>(async period => await SelectEvolutionPeriodAsync(period));
        DiscoverPremiumCommand = new Command(async () => await ShowPremiumTeaserAsync());
        UnlockPremiumCommand = new Command(async () => await ShowPremiumTeaserAsync());

        DateTime monthStart = GetCurrentMonthStart();
        PeriodDisplay = FormatMonth(monthStart);
        LastUpdatedDisplay = BuildLastUpdatedDisplay(DateTime.Now);
    }

    public ObservableCollection<CategoryStatsDto> CategoryStats { get; }

    public ICommand RefreshCommand { get; }

    public ICommand SelectEvolutionPeriodCommand { get; }

    public ICommand DiscoverPremiumCommand { get; }

    public ICommand UnlockPremiumCommand { get; }

    public bool IsPremiumUser
    {
        get => _isPremiumUser;
        private set
        {
            if (SetProperty(ref _isPremiumUser, value))
                OnPropertyChanged(nameof(ShowPremiumTeaser));
        }
    }

    public bool ShowPremiumTeaser => !IsPremiumUser;

    public MonthlyStatsDto MonthlyStats
    {
        get => _monthlyStats;
        private set => SetProperty(ref _monthlyStats, value);
    }

    public string PeriodDisplay
    {
        get => _periodDisplay;
        private set => SetProperty(ref _periodDisplay, value);
    }

    public string NetBalanceDisplay
    {
        get => _netBalanceDisplay;
        private set => SetProperty(ref _netBalanceDisplay, value);
    }

    public string ExpensesDisplay
    {
        get => _expensesDisplay;
        private set => SetProperty(ref _expensesDisplay, value);
    }

    public string ExpensesCountDisplay
    {
        get => _expensesCountDisplay;
        private set => SetProperty(ref _expensesCountDisplay, value);
    }

    public string IncomeHintDisplay
    {
        get => _incomeHintDisplay;
        private set => SetProperty(ref _incomeHintDisplay, value);
    }

    public string LastUpdatedDisplay
    {
        get => _lastUpdatedDisplay;
        private set => SetProperty(ref _lastUpdatedDisplay, value);
    }

    public bool HasCategoryChart
    {
        get => _hasCategoryChart;
        private set => SetProperty(ref _hasCategoryChart, value);
    }

    public bool HasMonthlyBarChart
    {
        get => _hasMonthlyBarChart;
        private set => SetProperty(ref _hasMonthlyBarChart, value);
    }

    public bool HasEvolutionChart
    {
        get => _hasEvolutionChart;
        private set => SetProperty(ref _hasEvolutionChart, value);
    }

    public string SelectedEvolutionPeriod
    {
        get => _selectedEvolutionPeriod;
        private set
        {
            if (SetProperty(ref _selectedEvolutionPeriod, value))
            {
                OnPropertyChanged(nameof(YearSegmentBackground));
                OnPropertyChanged(nameof(MonthSegmentBackground));
                OnPropertyChanged(nameof(YearSegmentTextColor));
                OnPropertyChanged(nameof(MonthSegmentTextColor));
                OnPropertyChanged(nameof(EvolutionSubtitle));
            }
        }
    }

    public string EvolutionSubtitle => string.Equals(SelectedEvolutionPeriod, "Mois", StringComparison.OrdinalIgnoreCase)
        ? "Dépenses par semaine sur le mois courant"
        : "Dépenses par mois sur l'année courante";

    public string YearSegmentBackground => EvolutionSegmentBackground("Année");

    public string MonthSegmentBackground => EvolutionSegmentBackground("Mois");

    public string YearSegmentTextColor => EvolutionSegmentTextColor("Année");

    public string MonthSegmentTextColor => EvolutionSegmentTextColor("Mois");

    public Chart CategoryPieChart
    {
        get => _categoryPieChart;
        private set => SetProperty(ref _categoryPieChart, value);
    }

    public Chart CurrentMonthBarChart
    {
        get => _currentMonthBarChart;
        private set => SetProperty(ref _currentMonthBarChart, value);
    }

    public Chart EvolutionLineChart
    {
        get => _evolutionLineChart;
        private set => SetProperty(ref _evolutionLineChart, value);
    }

    public Task InitializeAsync()
        => LoadAsync();

    public async Task LoadAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
            {
                ApplyEmptyState(GetCurrentMonthStart());
                return;
            }

            DateTime monthStart = GetCurrentMonthStart();
            DateTime nextMonthStart = monthStart.AddMonths(1);
            DateTime inclusiveMonthEnd = nextMonthStart.AddTicks(-1);
            DateTime yearStart = new(monthStart.Year, 1, 1);
            DateTime nextYearStart = yearStart.AddYears(1);

            PeriodDisplay = FormatMonth(monthStart);

            var expensesResult = await _expenseService.GetExpensesByPeriodAsync(
                CurrentUserId,
                monthStart,
                inclusiveMonthEnd);

            if (!expensesResult.IsSuccess)
            {
                ApplyEmptyState(monthStart);
                ErrorMessage = expensesResult.Message;
                return;
            }

            var yearExpensesResult = await _expenseService.GetExpensesByPeriodAsync(
                CurrentUserId,
                yearStart,
                nextYearStart.AddTicks(-1));

            var budgetsResult = await _budgetService.GetBudgetsAsync(CurrentUserId);
            if (!budgetsResult.IsSuccess)
            {
                ApplyEmptyState(monthStart);
                ErrorMessage = budgetsResult.Message;
                return;
            }

            var categoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);

            List<Expense> expenses = NormalizeExpenses(expensesResult.Data);
            List<Expense> yearExpenses = yearExpensesResult.IsSuccess
                ? NormalizeExpenses(yearExpensesResult.Data)
                : expenses;
            List<Budget> budgets = NormalizeBudgets(budgetsResult.Data);
            List<Category> categories = categoriesResult.IsSuccess
                ? NormalizeCategories(categoriesResult.Data)
                : [];

            MonthlyStatsDto monthlyStats = BuildMonthlyStats(expenses, budgets, monthStart, nextMonthStart);
            List<CategoryStatsDto> categoryStats = BuildCategoryStats(expenses, categories, MaxVisibleCategories);

            ApplyStats(monthlyStats, categoryStats, expenses, yearExpenses, monthStart, yearStart);

            if (!yearExpensesResult.IsSuccess)
                ErrorMessage = yearExpensesResult.Message;
            else if (!categoriesResult.IsSuccess)
                ErrorMessage = categoriesResult.Message;
        }, "Une erreur est survenue lors du chargement des statistiques.");
    }

    public static MonthlyStatsDto BuildMonthlyStats(
        IEnumerable<Expense>? expenses,
        IEnumerable<Budget>? budgets,
        DateTime monthStart,
        DateTime nextMonthStart)
    {
        List<Expense> monthExpenses = NormalizeExpenses(expenses)
            .Where(expense => expense.DateOperation >= monthStart && expense.DateOperation < nextMonthStart)
            .ToList();

        decimal expenseAmount = monthExpenses.Sum(expense => expense.Amount);
        decimal incomeAmount = NormalizeBudgets(budgets)
            .Where(budget => IsBudgetInPeriod(budget, monthStart, nextMonthStart))
            .Sum(budget => budget.Amount);

        return new MonthlyStatsDto
        {
            PeriodStart = monthStart.Date,
            PeriodEnd = nextMonthStart.AddDays(-1).Date,
            IncomeAmount = incomeAmount,
            ExpenseAmount = expenseAmount,
            NetBalance = incomeAmount - expenseAmount,
            ExpensesCount = monthExpenses.Count,
            HasIncomeSource = incomeAmount > 0
        };
    }

    public static List<CategoryStatsDto> BuildCategoryStats(
        IEnumerable<Expense>? expenses,
        IEnumerable<Category>? categories,
        int maxVisibleCategories = MaxVisibleCategories)
    {
        if (maxVisibleCategories <= 0)
            return [];

        List<Expense> normalizedExpenses = NormalizeExpenses(expenses);
        decimal totalExpenses = normalizedExpenses.Sum(expense => expense.Amount);

        if (totalExpenses <= 0)
            return [];

        Dictionary<int, Category> categoriesById = NormalizeCategories(categories)
            .GroupBy(category => category.Id)
            .ToDictionary(group => group.Key, group => group.First());

        List<CategoryStatsDto> groupedStats = normalizedExpenses
            .GroupBy(expense => expense.CategoryId)
            .Select(group =>
            {
                categoriesById.TryGetValue(group.Key, out Category? category);
                decimal amount = group.Sum(expense => expense.Amount);

                return new CategoryStatsDto
                {
                    CategoryId = group.Key,
                    CategoryName = string.IsNullOrWhiteSpace(category?.Name) ? "Catégorie inconnue" : category.Name,
                    CategoryColor = NormalizeColor(category?.Color),
                    CategoryIcon = category?.Icon ?? string.Empty,
                    Amount = amount,
                    Percentage = CalculatePercentage(amount, totalExpenses),
                    ExpensesCount = group.Count()
                };
            })
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (groupedStats.Count <= maxVisibleCategories)
            return groupedStats;

        int topCount = Math.Max(1, maxVisibleCategories - 1);
        List<CategoryStatsDto> visibleStats = groupedStats.Take(topCount).ToList();
        List<CategoryStatsDto> otherStats = groupedStats.Skip(topCount).ToList();
        decimal otherAmount = otherStats.Sum(item => item.Amount);
        int otherExpensesCount = otherStats.Sum(item => item.ExpensesCount);

        visibleStats.Add(new CategoryStatsDto
        {
            CategoryId = 0,
            CategoryName = OtherCategoryName,
            CategoryColor = PrimaryColor,
            Amount = otherAmount,
            Percentage = CalculatePercentage(otherAmount, totalExpenses),
            ExpensesCount = otherExpensesCount
        });

        return visibleStats;
    }

    public static decimal CalculatePercentage(decimal amount, decimal totalAmount)
    {
        if (amount <= 0 || totalAmount <= 0)
            return 0m;

        return Math.Round(amount / totalAmount * 100m, 1, MidpointRounding.AwayFromZero);
    }

    private void ApplyStats(
        MonthlyStatsDto monthlyStats,
        IReadOnlyList<CategoryStatsDto> categoryStats,
        IReadOnlyList<Expense> currentMonthExpenses,
        IReadOnlyList<Expense> currentYearExpenses,
        DateTime monthStart,
        DateTime yearStart)
    {
        MonthlyStats = monthlyStats;
        NetBalanceDisplay = CurrencyHelper.FormatSigned(monthlyStats.NetBalance, CurrentDevise);
        ExpensesDisplay = CurrencyHelper.Format(monthlyStats.ExpenseAmount, CurrentDevise);
        ExpensesCountDisplay = monthlyStats.ExpensesCount <= 1
            ? $"{monthlyStats.ExpensesCount} opération"
            : $"{monthlyStats.ExpensesCount} opérations";
        IncomeHintDisplay = monthlyStats.HasIncomeSource
            ? "Base: budgets actifs du mois"
            : "Budget mensuel non défini";
        LastUpdatedDisplay = BuildLastUpdatedDisplay(DateTime.Now);

        CategoryStats.Clear();
        foreach (CategoryStatsDto item in categoryStats)
            CategoryStats.Add(item);

        HasCategoryChart = CategoryStats.Count > 0;
        HasMonthlyBarChart = monthlyStats.ExpenseAmount > 0;
        CategoryPieChart = HasCategoryChart ? CreatePieChart(CategoryStats) : CreateEmptyPieChart();
        CurrentMonthBarChart = HasMonthlyBarChart
            ? CreateCurrentMonthBarChart(monthStart, currentMonthExpenses)
            : CreateEmptyBarChart();
        EvolutionLineChart = BuildEvolutionLineChart(SelectedEvolutionPeriod, monthStart, yearStart, currentMonthExpenses, currentYearExpenses);
        HasEvolutionChart = string.Equals(SelectedEvolutionPeriod, "Mois", StringComparison.OrdinalIgnoreCase)
            ? currentMonthExpenses.Any(expense => expense.Amount > 0)
            : currentYearExpenses.Any(expense => expense.Amount > 0);
    }

    private void ApplyEmptyState(DateTime monthStart)
    {
        MonthlyStats = BuildMonthlyStats([], [], monthStart, monthStart.AddMonths(1));
        NetBalanceDisplay = CurrencyHelper.FormatSigned(0m, CurrentDevise);
        ExpensesDisplay = CurrencyHelper.Format(0m, CurrentDevise);
        ExpensesCountDisplay = "0 opération";
        IncomeHintDisplay = "Budget mensuel non défini";
        LastUpdatedDisplay = BuildLastUpdatedDisplay(DateTime.Now);
        CategoryStats.Clear();
        HasCategoryChart = false;
        HasMonthlyBarChart = false;
        HasEvolutionChart = false;
        CategoryPieChart = CreateEmptyPieChart();
        CurrentMonthBarChart = CreateEmptyBarChart();
        EvolutionLineChart = CreateEmptyLineChart();
    }

    private async Task SelectEvolutionPeriodAsync(string? period)
    {
        string normalizedPeriod = string.Equals(period, "Mois", StringComparison.OrdinalIgnoreCase) ? "Mois" : "Année";
        if (string.Equals(SelectedEvolutionPeriod, normalizedPeriod, StringComparison.OrdinalIgnoreCase))
            return;

        SelectedEvolutionPeriod = normalizedPeriod;
        await LoadAsync();
    }

    private async Task ShowPremiumTeaserAsync()
    {
        await DialogService.ShowAlertAsync(
            "Premium",
            "Les analyses avancées, les exports et les prévisions seront débloqués dans la version Premium.",
            "Compris");
    }

    private static Chart CreatePieChart(IEnumerable<CategoryStatsDto> categories)
    {
        return new PieChart
        {
            Entries = categories.Select(CreateChartEntry).ToList(),
            BackgroundColor = SKColors.Transparent,
            LabelTextSize = 28
        };
    }

    private static Chart CreateCurrentMonthBarChart(DateTime monthStart, IEnumerable<Expense> expenses)
    {
        List<ChartEntry> entries = BuildWeeklyEntries(monthStart, NormalizeExpenses(expenses));

        return new BarChart
        {
            Entries = entries,
            BackgroundColor = SKColors.Transparent,
            LabelTextSize = 26,
            ValueLabelOrientation = Orientation.Horizontal,
            LabelOrientation = Orientation.Horizontal,
            Margin = 16
        };
    }

    private static Chart BuildEvolutionLineChart(
        string selectedPeriod,
        DateTime monthStart,
        DateTime yearStart,
        IEnumerable<Expense> currentMonthExpenses,
        IEnumerable<Expense> currentYearExpenses)
        => string.Equals(selectedPeriod, "Mois", StringComparison.OrdinalIgnoreCase)
            ? CreateCurrentMonthLineChart(monthStart, currentMonthExpenses)
            : CreateCurrentYearLineChart(yearStart, currentYearExpenses);

    private static Chart CreateCurrentYearLineChart(DateTime yearStart, IEnumerable<Expense> expenses)
    {
        decimal[] monthlyAmounts = new decimal[12];

        foreach (Expense expense in NormalizeExpenses(expenses))
        {
            if (expense.DateOperation.Year != yearStart.Year)
                continue;

            monthlyAmounts[expense.DateOperation.Month - 1] += expense.Amount;
        }

        string[] monthLabels = ["Jan", "Fév", "Mar", "Avr", "Mai", "Juin", "Juil", "Aoû", "Sep", "Oct", "Nov", "Déc"];
        return CreateLineChart(monthlyAmounts.Select((amount, index) => CreateEvolutionEntry(amount, monthLabels[index], index)));
    }

    private static Chart CreateCurrentMonthLineChart(DateTime monthStart, IEnumerable<Expense> expenses)
        => CreateLineChart(BuildWeeklyEntries(monthStart, expenses));

    private static Chart CreateLineChart(IEnumerable<ChartEntry> entries)
        => new LineChart
        {
            Entries = entries.ToList(),
            BackgroundColor = SKColors.Transparent,
            LabelTextSize = 24,
            ValueLabelOrientation = Orientation.Horizontal,
            LabelOrientation = Orientation.Horizontal,
            LineMode = LineMode.Straight,
            LineSize = 5,
            PointMode = PointMode.Circle,
            PointSize = 12,
            Margin = 18
        };

    private static List<ChartEntry> BuildWeeklyEntries(DateTime monthStart, IEnumerable<Expense> expenses)
    {
        int daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        int weeksCount = (int)Math.Ceiling(daysInMonth / 7d);
        decimal[] weeklyAmounts = new decimal[weeksCount];

        foreach (Expense expense in expenses)
        {
            if (expense.DateOperation.Year != monthStart.Year || expense.DateOperation.Month != monthStart.Month)
                continue;

            int weekIndex = Math.Min((expense.DateOperation.Day - 1) / 7, weeksCount - 1);
            weeklyAmounts[weekIndex] += expense.Amount;
        }

        return weeklyAmounts
            .Select((amount, index) => new ChartEntry((float)amount)
            {
                Label = $"S{index + 1}",
                ValueLabel = amount <= 0 ? string.Empty : amount.ToString("0.##", CultureInfo.InvariantCulture),
                Color = SKColor.Parse(index % 2 == 0 ? PrimaryColor : SuccessColor),
                TextColor = SKColor.Parse("#2B2B2B")
            })
            .ToList();
    }

    private static ChartEntry CreateEvolutionEntry(decimal amount, string label, int index)
        => new((float)amount)
        {
            Label = label,
            ValueLabel = amount <= 0 ? string.Empty : amount.ToString("0.##", CultureInfo.InvariantCulture),
            Color = SKColor.Parse(index % 2 == 0 ? PrimaryColor : SuccessColor),
            TextColor = SKColor.Parse("#2B2B2B")
        };

    private static ChartEntry CreateChartEntry(CategoryStatsDto category)
    {
        return new ChartEntry((float)category.Amount)
        {
            Label = category.CategoryName,
            ValueLabel = $"{category.Percentage:0.#} %",
            Color = SKColor.Parse(NormalizeColor(category.CategoryColor)),
            TextColor = SKColor.Parse("#2B2B2B")
        };
    }

    private static Chart CreateEmptyPieChart()
        => new PieChart
        {
            Entries = [],
            BackgroundColor = SKColors.Transparent
        };

    private static Chart CreateEmptyBarChart()
        => new BarChart
        {
            Entries = [],
            BackgroundColor = SKColors.Transparent
        };

    private static Chart CreateEmptyLineChart()
        => new LineChart
        {
            Entries = [],
            BackgroundColor = SKColors.Transparent
        };

    private string EvolutionSegmentBackground(string segment)
        => string.Equals(SelectedEvolutionPeriod, segment, StringComparison.OrdinalIgnoreCase) ? PrimaryColor : "#E8EEF3";

    private string EvolutionSegmentTextColor(string segment)
        => string.Equals(SelectedEvolutionPeriod, segment, StringComparison.OrdinalIgnoreCase) ? "#FFFFFF" : "#222222";

    private static List<Expense> NormalizeExpenses(IEnumerable<Expense>? expenses)
        => expenses?
            .Where(expense => expense is not null && expense.Amount > 0 && expense.DateOperation != default)
            .ToList() ?? [];

    private static List<Budget> NormalizeBudgets(IEnumerable<Budget>? budgets)
        => budgets?
            .Where(budget => budget is not null && budget.Amount > 0 && budget.StartDate != default && budget.IsActive)
            .ToList() ?? [];

    private static List<Category> NormalizeCategories(IEnumerable<Category>? categories)
        => categories?
            .Where(category => category is not null && category.Id > 0)
            .ToList() ?? [];

    private static bool IsBudgetInPeriod(Budget budget, DateTime monthStart, DateTime nextMonthStart)
    {
        DateTime budgetEnd = budget.EndDate?.Date ?? DateTime.MaxValue.Date;
        return budget.IsActive &&
               budget.Amount > 0 &&
               budget.StartDate.Date < nextMonthStart &&
               budgetEnd >= monthStart.Date;
    }

    private static string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return PrimaryColor;

        try
        {
            _ = SKColor.Parse(color);
            return color;
        }
        catch
        {
            return PrimaryColor;
        }
    }

    private static DateTime GetCurrentMonthStart()
    {
        DateTime today = DateTime.Today;
        return new DateTime(today.Year, today.Month, 1);
    }

    private static string FormatMonth(DateTime monthStart)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("fr-FR");
        string monthText = monthStart.ToString("MMMM yyyy", culture);
        return string.IsNullOrWhiteSpace(monthText)
            ? string.Empty
            : char.ToUpper(monthText[0], culture) + monthText[1..];
    }

    private static string BuildLastUpdatedDisplay(DateTime dateTime)
        => $"Mis à jour à {dateTime:HH:mm}";
}
