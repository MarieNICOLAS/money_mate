using System.Collections.ObjectModel;
using System.Windows.Input;
using MoneyMate.Configuration;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Models.DTOs;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Expenses;

public sealed class ExpensesListViewModel : AuthenticatedViewModelBase
{
    private const string BudgetRequiredMessage = "Créez d'abord un budget avant d'ajouter une dépense.";
    private const AppDataChangeKind RefreshChangeKinds = AppDataChangeKind.Expenses | AppDataChangeKind.Categories;

    private readonly IExpenseService _expenseService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService? _categoryService;
    private readonly IExpenseFilterStateService _filterStateService;
    private readonly IAppEventBus _appEventBus;

    private ExpenseSummaryDto _summary = new();
    private ExpenseFilterDto _activeFilter = new();
    private string _searchText = string.Empty;
    private string _selectedOperationType = "Toutes";
    private DateTime _selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private string _selectedSortLabel = "Date (récent)";
    private int _activeFiltersCount;
    private bool _isSearchVisible;
    private long _lastRefreshVersion = -1;
    private long _lastFilterVersion = -1;
    private CancellationTokenSource? _searchDebounceCts;

    public ExpensesListViewModel(
        IExpenseService expenseService,
        IBudgetService budgetService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService,
        IExpenseFilterStateService? filterStateService = null,
        IAppEventBus? appEventBus = null)
        : base(authenticationService, dialogService, navigationService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _filterStateService = filterStateService ?? new MoneyMate.Services.Implementations.ExpenseFilterStateService();
        _appEventBus = appEventBus ?? NullAppEventBus.Instance;

        Title = "Dépenses";
        Expenses = [];
        ActiveFilter = BuildMonthFilter();

        LoadExpensesCommand = new Command(async () => await LoadExpensesAsync());
        RefreshCommand = new Command(async () => await LoadExpensesAsync());
        SearchCommand = new Command(async () => await LoadExpensesAsync());
        ToggleSearchCommand = new Command(ToggleSearch);
        SelectOperationTypeCommand = new Command<string>(async type => await SelectOperationTypeAsync(type));
        PreviousMonthCommand = new Command(async () => await ChangeMonthAsync(-1));
        NextMonthCommand = new Command(async () => await ChangeMonthAsync(1));
        OpenFiltersCommand = new Command(async () => await OpenFiltersAsync());
        ChangeSortCommand = new Command(async () => await ChangeSortAsync());
        OpenExpenseDetailsCommand = new Command<ExpenseListItemDto>(async expense => await OpenExpenseDetailsAsync(expense));
        AddExpenseCommand = new Command(async () => await NavigateToExpenseCreationAsync(AppRoutes.AddExpense));
        QuickAddExpenseCommand = new Command(async () => await NavigateToExpenseCreationAsync(AppRoutes.QuickAddExpense));
        EditExpenseCommand = new Command<ExpenseListItemDto>(async expense => await EditExpenseAsync(expense));
        DeleteExpenseCommand = new Command<ExpenseListItemDto>(async expense => await DeleteExpenseAsync(expense));
    }

    public ExpensesListViewModel(
        IExpenseService expenseService,
        IBudgetService budgetService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService,
        IExpenseFilterStateService? filterStateService = null,
        IAppEventBus? appEventBus = null)
        : this(
            expenseService,
            budgetService,
            authenticationService,
            dialogService,
            navigationService,
            filterStateService,
            appEventBus)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    public ObservableCollection<ExpenseListItemDto> Expenses { get; }

    public ExpenseSummaryDto Summary
    {
        get => _summary;
        private set
        {
            if (SetProperty(ref _summary, value))
            {
                OnPropertyChanged(nameof(TotalExpensesDisplay));
                OnPropertyChanged(nameof(TotalIncomeDisplay));
                OnPropertyChanged(nameof(BalanceDisplay));
                OnPropertyChanged(nameof(TopCategories));
            }
        }
    }

    public ExpenseFilterDto ActiveFilter
    {
        get => _activeFilter;
        private set => SetProperty(ref _activeFilter, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                DebounceSearch();
        }
    }

    public string SelectedOperationType
    {
        get => _selectedOperationType;
        private set
        {
            if (SetProperty(ref _selectedOperationType, value))
                NotifySegmentStates();
        }
    }

    public DateTime SelectedMonth
    {
        get => _selectedMonth;
        private set
        {
            if (SetProperty(ref _selectedMonth, new DateTime(value.Year, value.Month, 1)))
                OnPropertyChanged(nameof(SelectedMonthLabel));
        }
    }

    public string SelectedMonthLabel => SelectedMonth.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));

    public string SelectedSortLabel
    {
        get => _selectedSortLabel;
        private set => SetProperty(ref _selectedSortLabel, value);
    }

    public int ActiveFiltersCount
    {
        get => _activeFiltersCount;
        private set
        {
            if (SetProperty(ref _activeFiltersCount, value))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
                OnPropertyChanged(nameof(ActiveFiltersBadgeText));
                OnPropertyChanged(nameof(FilterButtonText));
            }
        }
    }

    public bool HasActiveFilters => ActiveFiltersCount > 0;

    public string ActiveFiltersBadgeText => ActiveFiltersCount.ToString();

    public string FilterButtonText => HasActiveFilters ? $"Filtrer ({ActiveFiltersCount})" : "Filtrer";

    public bool HasExpenses => Expenses.Count > 0;

    public bool IsEmptyState => !HasExpenses && !IsBusy;

    public decimal TotalExpenses => Summary.TotalExpenses;

    public int ExpensesCount => Expenses.Count;

    public string Devise => CurrentDevise;

    public bool IsSearchVisible
    {
        get => _isSearchVisible;
        private set => SetProperty(ref _isSearchVisible, value);
    }

    public string ResultsLabel => $"{Expenses.Count} opération{(Expenses.Count > 1 ? "s" : string.Empty)}";

    public string TotalExpensesDisplay => CurrencyHelper.Format(Summary.TotalExpenses, CurrentDevise);

    public string TotalIncomeDisplay => CurrencyHelper.Format(Summary.TotalIncome, CurrentDevise);

    public string BalanceDisplay => CurrencyHelper.Format(Summary.Balance, CurrentDevise);

    public IReadOnlyList<CategorySummaryDto> TopCategories => Summary.TopCategories;

    public string AllSegmentBackground => SegmentBackground("Toutes");
    public string ExpensesSegmentBackground => SegmentBackground("Dépenses");
    public string IncomeSegmentBackground => SegmentBackground("Revenus");
    public string TransfersSegmentBackground => SegmentBackground("Transferts");
    public string AllSegmentTextColor => SegmentTextColor("Toutes");
    public string ExpensesSegmentTextColor => SegmentTextColor("Dépenses");
    public string IncomeSegmentTextColor => SegmentTextColor("Revenus");
    public string TransfersSegmentTextColor => SegmentTextColor("Transferts");

    public ICommand LoadExpensesCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ToggleSearchCommand { get; }
    public ICommand SelectOperationTypeCommand { get; }
    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand OpenFiltersCommand { get; }
    public ICommand ChangeSortCommand { get; }
    public ICommand OpenExpenseDetailsCommand { get; }
    public ICommand AddExpenseCommand { get; }
    public ICommand QuickAddExpenseCommand { get; }
    public ICommand EditExpenseCommand { get; }
    public ICommand DeleteExpenseCommand { get; }

    public Task LoadAsync() => LoadLegacyAsync();

    public async Task RefreshIfNeededAsync()
    {
        bool filterChanged = _filterStateService.Version != _lastFilterVersion;
        if (_lastRefreshVersion < 0 || filterChanged || _appEventBus.HasChangedSince(RefreshChangeKinds, _lastRefreshVersion))
            await LoadExpensesAsync(filterChanged);
    }

    public async Task LoadExpensesAsync(bool readSharedFilter = false)
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
            {
                ResetList();
                return;
            }

            if (readSharedFilter)
            {
                ActiveFilter = _filterStateService.CurrentFilter;
                ApplyFilterToPageState(ActiveFilter);
            }

            ExpenseFilterDto filter = BuildEffectiveFilter();
            IReadOnlyList<ExpenseListItemDto> expenses = await _expenseService.GetExpensesAsync(CurrentUserId, filter);
            ExpenseSummaryDto summary = await _expenseService.GetExpenseSummaryAsync(CurrentUserId, filter);

            Expenses.Clear();
            foreach (ExpenseListItemDto expense in expenses)
                Expenses.Add(expense);

            ActiveFilter = filter;
            Summary = summary;
            ActiveFiltersCount = CountActiveFilters(filter);
            _lastRefreshVersion = _appEventBus.GetVersion(RefreshChangeKinds);
            _lastFilterVersion = _filterStateService.Version;

            RefreshCollectionState();
        }, "Une erreur est survenue lors du chargement des dépenses.");
    }

    private async Task LoadLegacyAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
            {
                ResetList();
                return;
            }

            if (_categoryService is null)
            {
                await LoadExpensesAsync();
                return;
            }

            var expensesResult = await _expenseService.GetExpensesAsync(CurrentUserId);
            if (!expensesResult.IsSuccess)
            {
                ErrorMessage = expensesResult.Message;
                ResetList();
                return;
            }

            var categoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);
            if (!categoriesResult.IsSuccess)
            {
                ErrorMessage = categoriesResult.Message;
                ResetList();
                return;
            }

            Dictionary<int, Category> categoriesById = (categoriesResult.Data ?? [])
                .GroupBy(category => category.Id)
                .Select(group => group.First())
                .ToDictionary(category => category.Id, category => category);

            List<Expense> expenses = (expensesResult.Data ?? [])
                .OrderByDescending(expense => expense.DateOperation)
                .ThenByDescending(expense => expense.Id)
                .ToList();

            Expenses.Clear();
            foreach (Expense expense in expenses)
                Expenses.Add(ExpenseListItemViewModel.FromModel(expense, categoriesById, Devise));

            Summary = new ExpenseSummaryDto
            {
                TotalExpenses = expenses.Sum(expense => expense.Amount),
                Balance = -expenses.Sum(expense => expense.Amount),
                TopCategories = []
            };

            RefreshCollectionState();
        }, "Une erreur est survenue lors du chargement des dépenses.");
    }

    private void ToggleSearch()
    {
        IsSearchVisible = !IsSearchVisible;
        if (!IsSearchVisible && !string.IsNullOrWhiteSpace(SearchText))
            SearchText = string.Empty;
    }

    private async Task SelectOperationTypeAsync(string? type)
    {
        SelectedOperationType = string.IsNullOrWhiteSpace(type) ? "Toutes" : type;
        await LoadExpensesAsync();
    }

    private async Task ChangeMonthAsync(int offset)
    {
        SelectedMonth = SelectedMonth.AddMonths(offset);
        ActiveFilter = BuildMonthFilter();
        _filterStateService.SetFilter(ActiveFilter);
        await LoadExpensesAsync();
    }

    private async Task OpenFiltersAsync()
    {
        _filterStateService.SetFilter(BuildEffectiveFilter());
        _lastFilterVersion = _filterStateService.Version;
        await NavigationService.NavigateToAsync(AppRoutes.ExpenseFilter);
    }

    private async Task ChangeSortAsync()
    {
        (string sortBy, bool descending, string label) = SelectedSortLabel switch
        {
            "Date (récent)" => ("Date", false, "Date (ancien)"),
            "Date (ancien)" => ("Montant", true, "Montant décroissant"),
            "Montant décroissant" => ("Montant", false, "Montant croissant"),
            "Montant croissant" => ("Catégorie", false, "Catégorie A-Z"),
            _ => ("Date", true, "Date (récent)")
        };

        SelectedSortLabel = label;
        ActiveFilter.SortBy = sortBy;
        ActiveFilter.SortDescending = descending;
        await LoadExpensesAsync();
    }

    private async Task OpenExpenseDetailsAsync(ExpenseListItemDto? expense)
    {
        if (expense is null)
            return;

        await NavigationService.NavigateToAsync(AppRoutes.ExpenseDetails, new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = expense.Id
        });
    }

    private async Task EditExpenseAsync(ExpenseListItemDto? expense)
    {
        if (expense is null)
            return;

        await NavigationService.NavigateToAsync(AppRoutes.EditExpense, new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = expense.Id
        });
    }

    private async Task DeleteExpenseAsync(ExpenseListItemDto? expense)
    {
        if (expense is null || !EnsureCurrentUser())
            return;

        bool confirmed = await DialogService.ShowConfirmationAsync(
            "Supprimer l'opération",
            $"Supprimer « {expense.Title} » ?",
            "Supprimer",
            "Annuler");

        if (!confirmed)
            return;

        var result = await _expenseService.DeleteExpenseAsync(expense.Id, CurrentUserId);
        if (!result.IsSuccess)
        {
            await DialogService.ShowAlertAsync("Suppression", result.Message, "OK");
            return;
        }

        _appEventBus.PublishDataChanged(AppDataChangeKind.Expenses);
        await LoadExpensesAsync();
    }

    private async Task NavigateToExpenseCreationAsync(string route)
    {
        if (!EnsureCurrentUser())
            return;

        var result = await _budgetService.GetBudgetsAsync(CurrentUserId);
        if (!result.IsSuccess)
        {
            await DialogService.ShowAlertAsync("Budget", result.Message, "OK");
            return;
        }

        bool hasActiveBudget = (result.Data ?? []).Any(budget => budget.IsActive);
        if (!hasActiveBudget)
        {
            await DialogService.ShowAlertAsync("Budget requis", BudgetRequiredMessage, "OK");
            return;
        }

        await NavigationService.NavigateToAsync(route);
    }

    private ExpenseFilterDto BuildEffectiveFilter()
    {
        ExpenseFilterDto filter = ActiveFilter.Clone();
        filter.OperationType = SelectedOperationType;
        filter.SearchText = SearchText;
        filter.SortBy = ResolveSortBy(SelectedSortLabel);
        filter.SortDescending = !SelectedSortLabel.Contains("ancien", StringComparison.OrdinalIgnoreCase) &&
                                !SelectedSortLabel.Contains("croissant", StringComparison.OrdinalIgnoreCase) &&
                                !SelectedSortLabel.Contains("A-Z", StringComparison.OrdinalIgnoreCase);

        if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
        {
            filter.StartDate = SelectedMonth;
            filter.EndDate = SelectedMonth.AddMonths(1).AddDays(-1);
        }

        return filter;
    }

    private ExpenseFilterDto BuildMonthFilter()
        => new()
        {
            StartDate = SelectedMonth,
            EndDate = SelectedMonth.AddMonths(1).AddDays(-1),
            OperationType = SelectedOperationType,
            SearchText = SearchText,
            SortBy = ResolveSortBy(SelectedSortLabel),
            SortDescending = true
        };

    private void ApplyFilterToPageState(ExpenseFilterDto filter)
    {
        SelectedOperationType = string.IsNullOrWhiteSpace(filter.OperationType) ? "Toutes" : filter.OperationType;
        SearchText = filter.SearchText;

        if (filter.StartDate.HasValue)
            SelectedMonth = new DateTime(filter.StartDate.Value.Year, filter.StartDate.Value.Month, 1);

        SelectedSortLabel = filter.SortBy switch
        {
            "Montant" => filter.SortDescending ? "Montant décroissant" : "Montant croissant",
            "Catégorie" => "Catégorie A-Z",
            _ => filter.SortDescending ? "Date (récent)" : "Date (ancien)"
        };
    }

    private int CountActiveFilters(ExpenseFilterDto filter)
    {
        int count = 0;
        DateTime currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTime currentMonthEnd = currentMonth.AddMonths(1).AddDays(-1);

        if (filter.StartDate?.Date != currentMonth.Date || filter.EndDate?.Date != currentMonthEnd.Date)
            count++;
        if (!string.Equals(filter.OperationType, "Toutes", StringComparison.OrdinalIgnoreCase))
            count++;
        if (filter.CategoryIds.Count > 0)
            count++;
        if (filter.MinAmount.HasValue || filter.MaxAmount.HasValue)
            count++;
        if (!string.IsNullOrWhiteSpace(filter.PaymentMethod) && !string.Equals(filter.PaymentMethod, "Tous", StringComparison.OrdinalIgnoreCase))
            count++;
        if (filter.IsFixedCharge == true)
            count++;

        return count;
    }

    private void DebounceSearch()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        CancellationToken token = _searchDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                    await MainThread.InvokeOnMainThreadAsync(async () => await LoadExpensesAsync());
            }
            catch (TaskCanceledException)
            {
            }
        }, token);
    }

    private void ResetList()
    {
        Expenses.Clear();
        Summary = new ExpenseSummaryDto();
        RefreshCollectionState();
    }

    private void RefreshCollectionState()
    {
        OnPropertyChanged(nameof(HasExpenses));
        OnPropertyChanged(nameof(IsEmptyState));
        OnPropertyChanged(nameof(ResultsLabel));
        OnPropertyChanged(nameof(ExpensesCount));
        OnPropertyChanged(nameof(TotalExpenses));
        OnPropertyChanged(nameof(Devise));
        OnPropertyChanged(nameof(TotalExpensesDisplay));
        OnPropertyChanged(nameof(TotalIncomeDisplay));
        OnPropertyChanged(nameof(BalanceDisplay));
    }

    private void NotifySegmentStates()
    {
        OnPropertyChanged(nameof(AllSegmentBackground));
        OnPropertyChanged(nameof(ExpensesSegmentBackground));
        OnPropertyChanged(nameof(IncomeSegmentBackground));
        OnPropertyChanged(nameof(TransfersSegmentBackground));
        OnPropertyChanged(nameof(AllSegmentTextColor));
        OnPropertyChanged(nameof(ExpensesSegmentTextColor));
        OnPropertyChanged(nameof(IncomeSegmentTextColor));
        OnPropertyChanged(nameof(TransfersSegmentTextColor));
    }

    private string SegmentBackground(string segment)
        => string.Equals(SelectedOperationType, segment, StringComparison.OrdinalIgnoreCase) ? "#6793AE" : "#FFFFFF";

    private string SegmentTextColor(string segment)
        => string.Equals(SelectedOperationType, segment, StringComparison.OrdinalIgnoreCase) ? "#FFFFFF" : "#222222";

    private static string ResolveSortBy(string label)
        => label.Contains("Montant", StringComparison.OrdinalIgnoreCase)
            ? "Montant"
            : label.Contains("Catégorie", StringComparison.OrdinalIgnoreCase)
                ? "Catégorie"
                : "Date";
}
