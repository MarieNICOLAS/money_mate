using System.Collections.ObjectModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using MoneyMate.Configuration;
using MoneyMate.Helpers;
using MoneyMate.Models.DTOs;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Calendar;

public sealed class CalendarViewModel : AuthenticatedViewModelBase
{
    private const AppDataChangeKind RefreshChangeKinds =
        AppDataChangeKind.Expenses | AppDataChangeKind.FixedCharges | AppDataChangeKind.Categories;

    private readonly ICalendarService _calendarService;
    private readonly IAppEventBus _appEventBus;
    private readonly IExpenseFilterStateService _filterStateService;
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("fr-FR");
    private readonly List<CalendarOperationDto> _monthOperationCache = [];

    private DateTime _currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selectedDate = DateTime.Today.Date;
    private ExpenseFilterDto _activeFilter = new();
    private decimal _dailyExpenseTotal;
    private decimal _dailyIncomeTotal;
    private decimal _dailyBalance;
    private long _lastRefreshVersion = -1;
    private long _lastFilterVersion = -1;
    private string _dailyInfoText = "Aucune opération pour cette journée.";

    public CalendarViewModel(
        ICalendarService calendarService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService,
        IAppEventBus? appEventBus = null,
        IExpenseFilterStateService? filterStateService = null)
        : base(authenticationService, dialogService, navigationService)
    {
        _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
        _appEventBus = appEventBus ?? NullAppEventBus.Instance;
        _filterStateService = filterStateService ?? new MoneyMate.Services.Implementations.ExpenseFilterStateService();

        Title = "Calendrier";
        Days = [];
        DailyOperations = [];
        _activeFilter = BuildCalendarFilter();

        LoadMonthCommand = new Command(async () => await LoadMonthAsync());
        RefreshCommand = LoadMonthCommand;
        PreviousMonthCommand = new Command(async () => await ChangeMonthAsync(-1));
        NextMonthCommand = new Command(async () => await ChangeMonthAsync(1));
        GoToTodayCommand = new Command(async () => await GoToTodayAsync());
        SelectDayCommand = new Command<CalendarDayDto>(async day => await SelectDayAsync(day));
        OpenFiltersCommand = new Command(async () => await OpenFiltersAsync());
        OpenOperationDetailsCommand = new Command<CalendarOperationDto>(async operation => await OpenOperationDetailsAsync(operation));
    }

    public DateTime CurrentMonth
    {
        get => _currentMonth;
        set
        {
            DateTime normalized = new(value.Year, value.Month, 1);
            if (SetProperty(ref _currentMonth, normalized))
                OnPropertyChanged(nameof(CurrentMonthLabel));
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value.Date))
            {
                OnPropertyChanged(nameof(SelectedDateLabel));
                OnPropertyChanged(nameof(SelectedDateAccessibilityLabel));
            }
        }
    }

    public string CurrentMonthLabel => CurrentMonth.ToString("MMMM yyyy", _culture);

    public string SelectedDateLabel => SelectedDate.ToString("dddd dd MMMM yyyy", _culture);

    public string SelectedDateAccessibilityLabel => $"Résumé du {SelectedDateLabel}";

    public ObservableCollection<CalendarDayDto> Days { get; }

    public ObservableCollection<CalendarOperationDto> DailyOperations { get; }

    public decimal DailyExpenseTotal
    {
        get => _dailyExpenseTotal;
        private set
        {
            if (SetProperty(ref _dailyExpenseTotal, value))
                OnPropertyChanged(nameof(DailyExpenseTotalDisplay));
        }
    }

    public decimal DailyIncomeTotal
    {
        get => _dailyIncomeTotal;
        private set
        {
            if (SetProperty(ref _dailyIncomeTotal, value))
                OnPropertyChanged(nameof(DailyIncomeTotalDisplay));
        }
    }

    public decimal DailyBalance
    {
        get => _dailyBalance;
        private set
        {
            if (SetProperty(ref _dailyBalance, value))
            {
                OnPropertyChanged(nameof(DailyBalanceDisplay));
                OnPropertyChanged(nameof(DailyBalanceColor));
            }
        }
    }

    public string DailyExpenseTotalDisplay => CurrencyHelper.Format(DailyExpenseTotal, CurrentDevise);

    public string DailyIncomeTotalDisplay => CurrencyHelper.Format(DailyIncomeTotal, CurrentDevise);

    public string DailyBalanceDisplay => CurrencyHelper.Format(DailyBalance, CurrentDevise);

    public string DailyBalanceColor => DailyBalance >= 0 ? "#6CC57C" : "#E57373";

    public string DailyInfoText
    {
        get => _dailyInfoText;
        private set => SetProperty(ref _dailyInfoText, value);
    }

    public bool HasOperations => DailyOperations.Count > 0;

    public bool IsEmptyState => !HasOperations && !IsBusy;

    public ICommand LoadMonthCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand PreviousMonthCommand { get; }

    public ICommand NextMonthCommand { get; }

    public ICommand GoToTodayCommand { get; }

    public ICommand SelectDayCommand { get; }

    public ICommand OpenFiltersCommand { get; }

    public ICommand OpenOperationDetailsCommand { get; }

    public Task InitializeAsync() => RefreshIfNeededAsync();

    public async Task RefreshIfNeededAsync()
    {
        ExpenseFilterDto sharedFilter = NormalizeCalendarFilter(_filterStateService.CurrentFilter);
        bool filterChanged = _filterStateService.Version != _lastFilterVersion && !AreFiltersEquivalent(_activeFilter, sharedFilter);
        bool monthChanged = false;

        if (filterChanged)
        {
            monthChanged = ApplySharedFilter(sharedFilter);
            _lastFilterVersion = _filterStateService.Version;
        }

        if (_lastRefreshVersion < 0 || monthChanged || _appEventBus.HasChangedSince(RefreshChangeKinds, _lastRefreshVersion))
        {
            await LoadMonthAsync();
            return;
        }

        if (filterChanged)
            ApplyActiveFilterToView();
    }

    public async Task LoadMonthAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
            {
                ResetState();
                return;
            }

            IReadOnlyList<CalendarOperationDto> operations =
                await _calendarService.GetOperationsForMonthAsync(CurrentUserId, CurrentMonth);

            _monthOperationCache.Clear();
            _monthOperationCache.AddRange(operations);

            ApplyActiveFilterToView();
            _lastRefreshVersion = _appEventBus.GetVersion(RefreshChangeKinds);
            _lastFilterVersion = _filterStateService.Version;
        }, "Une erreur est survenue lors du chargement du calendrier.");
    }

    private async Task ChangeMonthAsync(int offset)
    {
        CurrentMonth = CurrentMonth.AddMonths(offset);
        _activeFilter.StartDate = CurrentMonth;
        _activeFilter.EndDate = CurrentMonth.AddMonths(1).AddDays(-1);

        if (SelectedDate.Year != CurrentMonth.Year || SelectedDate.Month != CurrentMonth.Month)
            SelectedDate = CurrentMonth;

        await LoadMonthAsync();
    }

    private async Task GoToTodayAsync()
    {
        DateTime today = DateTime.Today;
        CurrentMonth = new DateTime(today.Year, today.Month, 1);
        _activeFilter.StartDate = CurrentMonth;
        _activeFilter.EndDate = CurrentMonth.AddMonths(1).AddDays(-1);
        SelectedDate = today;
        await LoadMonthAsync();
    }

    private async Task SelectDayAsync(CalendarDayDto? day)
    {
        if (day is null)
            return;

        SelectedDate = day.Date.Date;

        if (SelectedDate.Year != CurrentMonth.Year || SelectedDate.Month != CurrentMonth.Month)
        {
            CurrentMonth = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
            _activeFilter.StartDate = CurrentMonth;
            _activeFilter.EndDate = CurrentMonth.AddMonths(1).AddDays(-1);
            await LoadMonthAsync();
            return;
        }

        BuildCalendarDays();
        RefreshSelectedDayFromCache();
    }

    private async Task OpenFiltersAsync()
    {
        _filterStateService.SetFilter(_activeFilter.Clone());
        _lastFilterVersion = _filterStateService.Version;
        await NavigationService.NavigateToAsync(AppRoutes.ExpenseFilter);
    }

    private async Task OpenOperationDetailsAsync(CalendarOperationDto? operation)
    {
        if (operation is null)
            return;

        if (operation.Id <= 0)
        {
            await DialogService.ShowAlertAsync("Charge fixe", "Cette opération est une charge fixe prévue. Elle sera consultable en détail une fois créée comme dépense.", "OK");
            return;
        }

        await NavigationService.NavigateToAsync(AppRoutes.ExpenseDetails, new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = operation.Id
        });
    }

    private void BuildCalendarDays()
    {
        Days.Clear();
        IReadOnlyList<CalendarOperationDto> filteredOperations = GetFilteredMonthOperations();

        DateTime firstOfMonth = new(CurrentMonth.Year, CurrentMonth.Month, 1);
        int mondayBasedOffset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        DateTime firstGridDate = firstOfMonth.AddDays(-mondayBasedOffset);

        for (int i = 0; i < 42; i++)
        {
            DateTime date = firstGridDate.AddDays(i).Date;
            List<CalendarOperationDto> dayOperations = filteredOperations
                .Where(operation => operation.Date.Date == date)
                .ToList();

            decimal expenses = dayOperations
                .Where(operation => operation.IsExpense || operation.IsFixedCharge)
                .Sum(operation => operation.Amount);
            decimal incomes = dayOperations
                .Where(operation => operation.IsIncome)
                .Sum(operation => operation.Amount);

            bool hasFixedCharge = dayOperations.Any(operation => operation.IsFixedCharge);
            bool hasIncome = dayOperations.Any(operation => operation.IsIncome);
            bool hasExpense = dayOperations.Any(operation => operation.IsExpense && !operation.IsFixedCharge);

            Days.Add(new CalendarDayDto
            {
                Date = date,
                DayNumber = date.Day,
                IsCurrentMonth = date.Month == CurrentMonth.Month && date.Year == CurrentMonth.Year,
                IsToday = date == DateTime.Today,
                IsSelected = date == SelectedDate,
                HasExpense = hasExpense,
                HasIncome = hasIncome,
                HasFixedCharge = hasFixedCharge,
                ExpenseTotal = expenses,
                IncomeTotal = incomes,
                Balance = incomes - expenses,
                DotColor = hasFixedCharge ? "#FFF6E9" : hasExpense ? "#E57373" : hasIncome ? "#6CC57C" : "Transparent"
            });
        }
    }

    private void RefreshSelectedDayFromCache()
    {
        List<CalendarOperationDto> operations = GetFilteredMonthOperations()
            .Where(operation => operation.Date.Date == SelectedDate.Date)
            .OrderByDescending(operation => operation.IsFixedCharge)
            .ThenBy(operation => operation.Type)
            .ThenBy(operation => operation.Title)
            .ToList();

        DailyOperations.Clear();
        foreach (CalendarOperationDto operation in operations)
            DailyOperations.Add(operation);

        DailyExpenseTotal = operations
            .Where(operation => operation.IsExpense || operation.IsFixedCharge)
            .Sum(operation => operation.Amount);
        DailyIncomeTotal = operations
            .Where(operation => operation.IsIncome)
            .Sum(operation => operation.Amount);
        DailyBalance = DailyIncomeTotal - DailyExpenseTotal;

        int fixedCharges = operations.Count(operation => operation.IsFixedCharge);
        int expenses = operations.Count(operation => operation.IsExpense && !operation.IsFixedCharge);
        int incomes = operations.Count(operation => operation.IsIncome);
        DailyInfoText = operations.Count == 0
            ? "Aucune opération pour cette journée."
            : BuildDailyInfoText(fixedCharges, expenses, incomes);

        OnPropertyChanged(nameof(HasOperations));
        OnPropertyChanged(nameof(IsEmptyState));
    }

    private void ApplyActiveFilterToView()
    {
        SelectedDate = ResolveSelectedDate(_activeFilter);
        BuildCalendarDays();
        RefreshSelectedDayFromCache();
    }

    private IReadOnlyList<CalendarOperationDto> GetFilteredMonthOperations()
    {
        IEnumerable<CalendarOperationDto> operations = _monthOperationCache;

        if (_activeFilter.StartDate.HasValue)
            operations = operations.Where(operation => operation.Date.Date >= _activeFilter.StartDate.Value.Date);

        if (_activeFilter.EndDate.HasValue)
            operations = operations.Where(operation => operation.Date.Date <= _activeFilter.EndDate.Value.Date);

        if (_activeFilter.CategoryIds.Count > 0)
            operations = operations.Where(operation => _activeFilter.CategoryIds.Contains(operation.CategoryId));

        if (_activeFilter.MinAmount.HasValue)
            operations = operations.Where(operation => operation.Amount >= _activeFilter.MinAmount.Value);

        if (_activeFilter.MaxAmount.HasValue)
            operations = operations.Where(operation => operation.Amount <= _activeFilter.MaxAmount.Value);

        if (_activeFilter.IsFixedCharge == true)
            operations = operations.Where(operation => operation.IsFixedCharge);

        if (!string.IsNullOrWhiteSpace(_activeFilter.OperationType) && !string.Equals(_activeFilter.OperationType, "Toutes", StringComparison.OrdinalIgnoreCase))
        {
            string operationType = SingularizeOperationType(_activeFilter.OperationType);
            operations = operations.Where(operation => string.Equals(operation.Type, operationType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(_activeFilter.SearchText))
        {
            string searchText = _activeFilter.SearchText.Trim();
            operations = operations.Where(operation =>
                operation.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                operation.CategoryName.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        return operations
            .OrderBy(operation => operation.Date)
            .ThenBy(operation => operation.Type)
            .ThenBy(operation => operation.Title)
            .ToList();
    }

    private bool ApplySharedFilter(ExpenseFilterDto filter)
    {
        _activeFilter = filter;

        DateTime targetMonth = new(filter.StartDate!.Value.Year, filter.StartDate.Value.Month, 1);
        bool monthChanged = CurrentMonth != targetMonth;
        CurrentMonth = targetMonth;
        SelectedDate = ResolveSelectedDate(filter);

        return monthChanged;
    }

    private ExpenseFilterDto BuildCalendarFilter()
        => NormalizeCalendarFilter(new ExpenseFilterDto
        {
            StartDate = CurrentMonth,
            EndDate = CurrentMonth.AddMonths(1).AddDays(-1),
            OperationType = "Toutes",
            PaymentMethod = "Tous"
        });

    private static ExpenseFilterDto NormalizeCalendarFilter(ExpenseFilterDto? filter)
    {
        ExpenseFilterDto normalized = filter?.Clone() ?? new ExpenseFilterDto();
        DateTime startDate = normalized.StartDate?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTime monthStart = new(startDate.Year, startDate.Month, 1);

        normalized.StartDate = startDate;
        normalized.EndDate ??= monthStart.AddMonths(1).AddDays(-1);
        normalized.OperationType = string.IsNullOrWhiteSpace(normalized.OperationType) ? "Toutes" : normalized.OperationType;
        normalized.PaymentMethod = string.IsNullOrWhiteSpace(normalized.PaymentMethod) ? "Tous" : normalized.PaymentMethod;
        normalized.SearchText ??= string.Empty;
        normalized.CategoryIds ??= [];

        return normalized;
    }

    private DateTime ResolveSelectedDate(ExpenseFilterDto filter)
    {
        DateTime monthStart = new(CurrentMonth.Year, CurrentMonth.Month, 1);
        DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);
        DateTime minDate = filter.StartDate.HasValue && filter.StartDate.Value.Date > monthStart
            ? filter.StartDate.Value.Date
            : monthStart;
        DateTime maxDate = filter.EndDate.HasValue && filter.EndDate.Value.Date < monthEnd
            ? filter.EndDate.Value.Date
            : monthEnd;

        if (maxDate < minDate)
            return minDate;

        DateTime today = DateTime.Today;
        if (today >= minDate && today <= maxDate && today.Month == CurrentMonth.Month && today.Year == CurrentMonth.Year)
            return today;

        if (SelectedDate >= minDate && SelectedDate <= maxDate)
            return SelectedDate;

        return minDate;
    }

    private static bool AreFiltersEquivalent(ExpenseFilterDto left, ExpenseFilterDto right)
        => Nullable.Equals(left.StartDate?.Date, right.StartDate?.Date)
            && Nullable.Equals(left.EndDate?.Date, right.EndDate?.Date)
            && string.Equals(left.OperationType, right.OperationType, StringComparison.OrdinalIgnoreCase)
            && left.CategoryIds.OrderBy(id => id).SequenceEqual(right.CategoryIds.OrderBy(id => id))
            && left.MinAmount == right.MinAmount
            && left.MaxAmount == right.MaxAmount
            && string.Equals(left.PaymentMethod, right.PaymentMethod, StringComparison.OrdinalIgnoreCase)
            && left.IsFixedCharge == right.IsFixedCharge
            && string.Equals(left.SearchText, right.SearchText, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.SortBy, right.SortBy, StringComparison.OrdinalIgnoreCase)
            && left.SortDescending == right.SortDescending;

    private static string SingularizeOperationType(string operationType)
        => operationType switch
        {
            "Dépenses" => "Dépense",
            "Revenus" => "Revenu",
            "Transferts" => "Transfert",
            _ => operationType
        };

    private static string BuildDailyInfoText(int fixedCharges, int expenses, int incomes)
    {
        List<string> parts = [];

        if (fixedCharges > 0)
            parts.Add($"{fixedCharges} prélèvement{(fixedCharges > 1 ? "s" : string.Empty)}");

        if (expenses > 0)
            parts.Add($"{expenses} dépense{(expenses > 1 ? "s" : string.Empty)}");

        if (incomes > 0)
            parts.Add($"{incomes} revenu{(incomes > 1 ? "s" : string.Empty)}");

        return parts.Count == 0
            ? "Aucune opération pour cette journée."
            : $"{string.Join(" et ", parts)} prévu{(parts.Count > 1 ? "s" : string.Empty)} ce jour.";
    }

    private void ResetState()
    {
        _monthOperationCache.Clear();
        Days.Clear();
        DailyOperations.Clear();
        DailyExpenseTotal = 0m;
        DailyIncomeTotal = 0m;
        DailyBalance = 0m;
        DailyInfoText = "Aucune opération pour cette journée.";
        OnPropertyChanged(nameof(HasOperations));
        OnPropertyChanged(nameof(IsEmptyState));
    }
}
