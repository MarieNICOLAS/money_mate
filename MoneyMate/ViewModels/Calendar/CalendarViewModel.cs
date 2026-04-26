using System.Collections.ObjectModel;
using System.Windows.Input;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Calendar;

public sealed class CalendarViewModel : AuthenticatedViewModelBase
{
    private const AppDataChangeKind RefreshChangeKinds =
        AppDataChangeKind.Expenses | AppDataChangeKind.FixedCharges | AppDataChangeKind.Categories;

    private readonly IExpenseService _expenseService;
    private readonly IFixedChargeService _fixedChargeService;
    private readonly ICategoryService _categoryService;
    private readonly IAppEventBus _appEventBus;

    private DateTime _displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private long _lastRefreshVersion = -1;
    private string _monthLabel = string.Empty;
    private string _monthPeriodLabel = string.Empty;
    private string _monthExpensesDisplay = CurrencyHelper.Format(0m);
    private string _monthFixedChargesDisplay = CurrencyHelper.Format(0m);

    public CalendarViewModel(
        IExpenseService expenseService,
        IFixedChargeService fixedChargeService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService,
        IAppEventBus? appEventBus = null)
        : base(authenticationService, dialogService, navigationService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _fixedChargeService = fixedChargeService ?? throw new ArgumentNullException(nameof(fixedChargeService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _appEventBus = appEventBus ?? NullAppEventBus.Instance;

        Title = "Calendrier";
        DayGroups = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        PreviousMonthCommand = new Command(async () => await ChangeMonthAsync(-1));
        NextMonthCommand = new Command(async () => await ChangeMonthAsync(1));
        CurrentMonthCommand = new Command(async () => await GoToCurrentMonthAsync());

        UpdateMonthLabels();
    }

    public ObservableCollection<CalendarDayGroupViewModel> DayGroups { get; }

    public ICommand RefreshCommand { get; }

    public ICommand PreviousMonthCommand { get; }

    public ICommand NextMonthCommand { get; }

    public ICommand CurrentMonthCommand { get; }

    public string MonthLabel
    {
        get => _monthLabel;
        private set => SetProperty(ref _monthLabel, value);
    }

    public string MonthPeriodLabel
    {
        get => _monthPeriodLabel;
        private set => SetProperty(ref _monthPeriodLabel, value);
    }

    public string MonthExpensesDisplay
    {
        get => _monthExpensesDisplay;
        private set => SetProperty(ref _monthExpensesDisplay, value);
    }

    public string MonthFixedChargesDisplay
    {
        get => _monthFixedChargesDisplay;
        private set => SetProperty(ref _monthFixedChargesDisplay, value);
    }

    public bool HasDayGroups => DayGroups.Count > 0;

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
            if (!EnsureCurrentUser())
            {
                ResetCalendar();
                return;
            }

            DateTime monthStart = new(_displayedMonth.Year, _displayedMonth.Month, 1);
            DateTime nextMonthStart = monthStart.AddMonths(1);
            DateTime monthEnd = nextMonthStart.AddDays(-1);

            var expensesResult = await _expenseService.GetExpensesByPeriodAsync(CurrentUserId, monthStart, monthEnd);
            if (!expensesResult.IsSuccess)
            {
                ResetCalendar();
                ErrorMessage = expensesResult.Message;
                return;
            }

            var fixedChargesResult = await _fixedChargeService.GetFixedChargesAsync(CurrentUserId);
            if (!fixedChargesResult.IsSuccess)
            {
                ResetCalendar();
                ErrorMessage = fixedChargesResult.Message;
                return;
            }

            var categoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);
            if (!categoriesResult.IsSuccess)
            {
                ResetCalendar();
                ErrorMessage = categoriesResult.Message;
                return;
            }

            Dictionary<int, Category> categoriesById = (categoriesResult.Data ?? [])
                .GroupBy(category => category.Id)
                .Select(group => group.First())
                .ToDictionary(category => category.Id, category => category);

            List<CalendarItemViewModel> calendarItems = [];
            calendarItems.AddRange(BuildExpenseItems(expensesResult.Data ?? [], categoriesById));
            calendarItems.AddRange(BuildFixedChargeItems(fixedChargesResult.Data ?? [], categoriesById, monthStart, nextMonthStart));

            MonthExpensesDisplay = CurrencyHelper.Format((expensesResult.Data ?? []).Sum(expense => expense.Amount), CurrentDevise);
            MonthFixedChargesDisplay = CurrencyHelper.Format(calendarItems
                .Where(item => item.ItemType == CalendarItemType.FixedCharge)
                .Sum(item => item.Amount), CurrentDevise);

            DayGroups.Clear();
            foreach (CalendarDayGroupViewModel group in calendarItems
                .OrderBy(item => item.Date)
                .ThenBy(item => item.ItemType)
                .GroupBy(item => item.Date.Date)
                .Select(group => CalendarDayGroupViewModel.Create(group.Key, group.ToList(), CurrentDevise)))
            {
                DayGroups.Add(group);
            }

            OnPropertyChanged(nameof(HasDayGroups));
            UpdateMonthLabels();
            UpdateObservedRefreshVersion();
        }, "Une erreur est survenue lors du chargement du calendrier.");
    }

    private void UpdateObservedRefreshVersion()
        => _lastRefreshVersion = _appEventBus.GetVersion(RefreshChangeKinds);

    private async Task ChangeMonthAsync(int offset)
    {
        _displayedMonth = _displayedMonth.AddMonths(offset);
        UpdateMonthLabels();
        await LoadAsync();
    }

    private async Task GoToCurrentMonthAsync()
    {
        _displayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        UpdateMonthLabels();
        await LoadAsync();
    }

    private void ResetCalendar()
    {
        DayGroups.Clear();
        MonthExpensesDisplay = CurrencyHelper.Format(0m, CurrentDevise);
        MonthFixedChargesDisplay = CurrencyHelper.Format(0m, CurrentDevise);
        OnPropertyChanged(nameof(HasDayGroups));
        UpdateMonthLabels();
    }

    private void UpdateMonthLabels()
    {
        DateTime monthStart = new(_displayedMonth.Year, _displayedMonth.Month, 1);
        DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

        MonthLabel = monthStart.ToString("MMMM yyyy");
        MonthPeriodLabel = $"{monthStart:dd/MM/yyyy} - {monthEnd:dd/MM/yyyy}";
    }

    private List<CalendarItemViewModel> BuildExpenseItems(
        IEnumerable<Expense> expenses,
        IReadOnlyDictionary<int, Category> categoriesById)
    {
        return expenses
            .OrderBy(expense => expense.DateOperation)
            .Select(expense =>
            {
                categoriesById.TryGetValue(expense.CategoryId, out Category? category);

                return new CalendarItemViewModel
                {
                    Date = expense.DateOperation.Date,
                    Title = string.IsNullOrWhiteSpace(expense.Note) ? category?.Name ?? "Dépense" : expense.Note,
                    Subtitle = expense.IsFixedCharge
                        ? $"Charge fixe • {category?.Name ?? "Catégorie"}"
                        : category?.Name ?? "Catégorie",
                    Amount = expense.Amount,
                    AmountDisplay = CurrencyHelper.Format(expense.Amount, CurrentDevise),
                    Icon = string.IsNullOrWhiteSpace(category?.Icon) ? "💸" : category!.Icon,
                    ItemType = expense.IsFixedCharge ? CalendarItemType.FixedChargeExpense : CalendarItemType.Expense
                };
            })
            .ToList();
    }

    private List<CalendarItemViewModel> BuildFixedChargeItems(
        IEnumerable<FixedCharge> fixedCharges,
        IReadOnlyDictionary<int, Category> categoriesById,
        DateTime monthStart,
        DateTime nextMonthStart)
    {
        List<CalendarItemViewModel> items = [];

        foreach (FixedCharge fixedCharge in fixedCharges.Where(charge => charge.IsActive))
        {
            categoriesById.TryGetValue(fixedCharge.CategoryId, out Category? category);

            foreach (DateTime occurrence in EnumerateOccurrencesInMonth(fixedCharge, monthStart, nextMonthStart))
            {
                items.Add(new CalendarItemViewModel
                {
                    Date = occurrence.Date,
                    Title = fixedCharge.Name,
                    Subtitle = $"Charge fixe prévue • {category?.Name ?? "Catégorie"}",
                    Amount = fixedCharge.Amount,
                    AmountDisplay = CurrencyHelper.Format(fixedCharge.Amount, CurrentDevise),
                    Icon = "📅",
                    ItemType = CalendarItemType.FixedCharge
                });
            }
        }

        return items;
    }

    private static IEnumerable<DateTime> EnumerateOccurrencesInMonth(FixedCharge fixedCharge, DateTime monthStart, DateTime nextMonthStart)
    {
        DateTime occurrence = fixedCharge.StartDate.Date;
        DateTime monthEnd = nextMonthStart.AddDays(-1);

        while (occurrence < nextMonthStart)
        {
            if (fixedCharge.EndDate.HasValue && occurrence.Date > fixedCharge.EndDate.Value.Date)
                yield break;

            if (occurrence >= monthStart && occurrence < nextMonthStart)
                yield return occurrence;

            occurrence = fixedCharge.Frequency switch
            {
                "Quarterly" => occurrence.AddMonths(3),
                "Yearly" => occurrence.AddYears(1),
                _ => occurrence.AddMonths(1)
            };

            if (occurrence > monthEnd && occurrence >= nextMonthStart)
                yield break;
        }
    }
}

public sealed class CalendarDayGroupViewModel
{
    public required string DayLabel { get; init; }

    public required string TotalAmountDisplay { get; init; }

    public required IReadOnlyList<CalendarItemViewModel> Items { get; init; }

    public static CalendarDayGroupViewModel Create(DateTime date, IReadOnlyList<CalendarItemViewModel> items, string devise)
    {
        decimal totalAmount = items.Sum(item => item.Amount);

        return new CalendarDayGroupViewModel
        {
            DayLabel = date.ToString("dddd dd MMMM"),
            TotalAmountDisplay = CurrencyHelper.Format(totalAmount, devise),
            Items = items
        };
    }
}

public sealed class CalendarItemViewModel
{
    public DateTime Date { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string AmountDisplay { get; init; } = string.Empty;

    public string Icon { get; init; } = "💸";

    public CalendarItemType ItemType { get; init; }
}

public enum CalendarItemType
{
    Expense = 0,
    FixedChargeExpense = 1,
    FixedCharge = 2
}
