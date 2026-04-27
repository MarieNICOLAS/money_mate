using System.Collections.ObjectModel;
using System.Windows.Input;
using MoneyMate.Models.DTOs;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Expenses;

public sealed class ExpenseFilterViewModel : AuthenticatedViewModelBase
{
    private readonly IExpenseFilterStateService _filterStateService;
    private readonly ICategoryService _categoryService;
    private readonly IReadOnlyList<string> _quarterOptions;

    private ExpenseFilterDto _filter = new();
    private string _selectedPeriodMode = "Mois";
    private DateTime _selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private int _selectedYear = DateTime.Today.Year;
    private string _selectedQuarter = $"T{((DateTime.Today.Month - 1) / 3) + 1} {DateTime.Today.Year}";
    private DateTime? _customStartDate;
    private DateTime? _customEndDate;
    private string _selectedOperationType = "Toutes";
    private decimal? _minAmount;
    private decimal? _maxAmount;
    private string _selectedPaymentMethod = "Tous";
    private bool _fixedChargesOnly;
    private int _activeFiltersCount;
    private bool _hasValidationError;
    private string _validationErrorMessage = string.Empty;
    private bool _isLoaded;

    public ExpenseFilterViewModel(
        IExpenseFilterStateService filterStateService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _filterStateService = filterStateService ?? throw new ArgumentNullException(nameof(filterStateService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _quarterOptions = Enumerable.Range(DateTime.Today.Year - 3, 7)
            .SelectMany(year => Enumerable.Range(1, 4).Select(quarter => $"T{quarter} {year}"))
            .ToList();

        Title = "Filtres";
        Categories = [];

        CloseCommand = new Command(async () => await NavigationService.GoBackAsync());
        ResetCommand = new Command(Reset);
        ApplyCommand = new Command(async () => await ApplyAsync());
        SelectPeriodModeCommand = new Command<string>(SelectPeriodMode);
        SelectOperationTypeCommand = new Command<string>(SelectOperationType);
        ToggleCategoryCommand = new Command<CategoryFilterItemDto>(ToggleCategory);
        ToggleSelectAllCategoriesCommand = new Command(ToggleSelectAllCategories);
        ValidateCommand = new Command(() => Validate());
    }

    public ExpenseFilterDto Filter
    {
        get => _filter;
        private set => SetProperty(ref _filter, value);
    }

    public ObservableCollection<CategoryFilterItemDto> Categories { get; }

    public IReadOnlyList<string> PaymentMethods { get; } =
    [
        "Tous",
        "Carte bancaire",
        "Espèces",
        "Virement",
        "Prélèvement",
        "Autre"
    ];

    public IReadOnlyList<string> QuarterOptions => _quarterOptions;

    public string SelectedPeriodMode
    {
        get => _selectedPeriodMode;
        set
        {
            if (SetProperty(ref _selectedPeriodMode, value))
            {
                OnPropertyChanged(nameof(IsMonthPeriod));
                OnPropertyChanged(nameof(IsQuarterPeriod));
                OnPropertyChanged(nameof(IsYearPeriod));
                OnPropertyChanged(nameof(IsCustomPeriod));
                NotifyPeriodSegments();
                RecalculateActiveFilters();
            }
        }
    }

    public DateTime SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (SetProperty(ref _selectedMonth, new DateTime(value.Year, value.Month, 1)))
                RecalculateActiveFilters();
        }
    }

    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value))
                RecalculateActiveFilters();
        }
    }

    public string SelectedQuarter
    {
        get => _selectedQuarter;
        set
        {
            if (SetProperty(ref _selectedQuarter, value))
                RecalculateActiveFilters();
        }
    }

    public DateTime? CustomStartDate
    {
        get => _customStartDate;
        set
        {
            if (SetProperty(ref _customStartDate, value))
            {
                OnPropertyChanged(nameof(CustomStartDateValue));
                RecalculateActiveFilters();
            }
        }
    }

    public DateTime? CustomEndDate
    {
        get => _customEndDate;
        set
        {
            if (SetProperty(ref _customEndDate, value))
            {
                OnPropertyChanged(nameof(CustomEndDateValue));
                RecalculateActiveFilters();
            }
        }
    }

    public DateTime CustomStartDateValue
    {
        get => CustomStartDate ?? SelectedMonth;
        set => CustomStartDate = value.Date;
    }

    public DateTime CustomEndDateValue
    {
        get => CustomEndDate ?? (CustomStartDate ?? SelectedMonth).Date;
        set => CustomEndDate = value.Date;
    }

    public string SelectedOperationType
    {
        get => _selectedOperationType;
        set
        {
            if (SetProperty(ref _selectedOperationType, value))
            {
                NotifyOperationSegments();
                RecalculateActiveFilters();
            }
        }
    }

    public decimal? MinAmount
    {
        get => _minAmount;
        set
        {
            if (SetProperty(ref _minAmount, value))
                RecalculateActiveFilters();
        }
    }

    public decimal? MaxAmount
    {
        get => _maxAmount;
        set
        {
            if (SetProperty(ref _maxAmount, value))
                RecalculateActiveFilters();
        }
    }

    public string SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (SetProperty(ref _selectedPaymentMethod, value))
                RecalculateActiveFilters();
        }
    }

    public bool FixedChargesOnly
    {
        get => _fixedChargesOnly;
        set
        {
            if (SetProperty(ref _fixedChargesOnly, value))
                RecalculateActiveFilters();
        }
    }

    public int ActiveFiltersCount
    {
        get => _activeFiltersCount;
        private set
        {
            if (SetProperty(ref _activeFiltersCount, value))
                OnPropertyChanged(nameof(ApplyButtonText));
        }
    }

    public bool IsCustomPeriod => string.Equals(SelectedPeriodMode, "Personnalisée", StringComparison.OrdinalIgnoreCase);
    public bool IsMonthPeriod => string.Equals(SelectedPeriodMode, "Mois", StringComparison.OrdinalIgnoreCase);
    public bool IsQuarterPeriod => string.Equals(SelectedPeriodMode, "Trimestre", StringComparison.OrdinalIgnoreCase);
    public bool IsYearPeriod => string.Equals(SelectedPeriodMode, "Année", StringComparison.OrdinalIgnoreCase);

    public bool HasValidationError
    {
        get => _hasValidationError;
        private set => SetProperty(ref _hasValidationError, value);
    }

    public string ValidationErrorMessage
    {
        get => _validationErrorMessage;
        private set => SetProperty(ref _validationErrorMessage, value);
    }

    public string SelectAllCategoriesText => Categories.All(category => category.IsSelected)
        ? "Tout désélectionner"
        : "Tout sélectionner";

    public string ApplyButtonText => $"Appliquer ({ActiveFiltersCount})";

    public string AllSegmentBackground => SegmentBackground("Toutes");
    public string ExpenseSegmentBackground => SegmentBackground("Dépenses");
    public string IncomeSegmentBackground => SegmentBackground("Revenus");
    public string TransferSegmentBackground => SegmentBackground("Transferts");
    public string AllSegmentTextColor => SegmentTextColor("Toutes");
    public string ExpenseSegmentTextColor => SegmentTextColor("Dépenses");
    public string IncomeSegmentTextColor => SegmentTextColor("Revenus");
    public string TransferSegmentTextColor => SegmentTextColor("Transferts");

    public string MonthPeriodBackground => PeriodBackground("Mois");
    public string QuarterPeriodBackground => PeriodBackground("Trimestre");
    public string YearPeriodBackground => PeriodBackground("Année");
    public string CustomPeriodBackground => PeriodBackground("Personnalisée");
    public string MonthPeriodTextColor => PeriodTextColor("Mois");
    public string QuarterPeriodTextColor => PeriodTextColor("Trimestre");
    public string YearPeriodTextColor => PeriodTextColor("Année");
    public string CustomPeriodTextColor => PeriodTextColor("Personnalisée");

    public ICommand CloseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand SelectPeriodModeCommand { get; }
    public ICommand SelectOperationTypeCommand { get; }
    public ICommand ToggleCategoryCommand { get; }
    public ICommand ToggleSelectAllCategoriesCommand { get; }
    public ICommand ValidateCommand { get; }

    public async Task LoadAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            Filter = NormalizeFilter(_filterStateService.CurrentFilter);
            ApplyFilter(Filter);

            if (!_isLoaded)
                await LoadCategoriesAsync();
            else
                SynchronizeCategorySelection();

            _isLoaded = true;
            RecalculateActiveFilters();
        }, "Une erreur est survenue lors du chargement des filtres.");
    }

    private void ApplyFilter(ExpenseFilterDto filter)
    {
        HasValidationError = false;
        ValidationErrorMessage = string.Empty;

        DateTime todayMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedMonth = filter.StartDate.HasValue
            ? new DateTime(filter.StartDate.Value.Year, filter.StartDate.Value.Month, 1)
            : todayMonth;
        SelectedYear = filter.StartDate?.Year ?? DateTime.Today.Year;
        SelectedQuarter = $"T{(((filter.StartDate?.Month ?? DateTime.Today.Month) - 1) / 3) + 1} {SelectedYear}";
        SelectedOperationType = string.IsNullOrWhiteSpace(filter.OperationType) ? "Toutes" : filter.OperationType;
        MinAmount = filter.MinAmount;
        MaxAmount = filter.MaxAmount;
        SelectedPaymentMethod = string.IsNullOrWhiteSpace(filter.PaymentMethod) ? "Tous" : filter.PaymentMethod;
        FixedChargesOnly = filter.IsFixedCharge == true;
        CustomStartDate = filter.StartDate;
        CustomEndDate = filter.EndDate;
        SelectedPeriodMode = ResolvePeriodMode(filter);

        if (!QuarterOptions.Contains(SelectedQuarter))
            SelectedQuarter = QuarterOptions.FirstOrDefault() ?? SelectedQuarter;
    }

    private void Reset()
    {
        Filter = new ExpenseFilterDto();
        SelectedPeriodMode = "Mois";
        SelectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedYear = DateTime.Today.Year;
        SelectedQuarter = $"T{((DateTime.Today.Month - 1) / 3) + 1} {DateTime.Today.Year}";
        CustomStartDate = null;
        CustomEndDate = null;
        SelectedOperationType = "Toutes";
        MinAmount = null;
        MaxAmount = null;
        SelectedPaymentMethod = "Tous";
        FixedChargesOnly = false;
        HasValidationError = false;
        ValidationErrorMessage = string.Empty;

        for (int i = 0; i < Categories.Count; i++)
        {
            CategoryFilterItemDto category = Categories[i];
            Categories[i] = category.WithSelected(true);
        }

        OnPropertyChanged(nameof(SelectAllCategoriesText));
        RecalculateActiveFilters();
    }

    private async Task ApplyAsync()
    {
        if (!Validate())
            return;

        ExpenseFilterDto filter = BuildFilter();
        _filterStateService.SetFilter(filter);
        await NavigationService.GoBackAsync();
    }

    private void SelectPeriodMode(string? mode)
    {
        SelectedPeriodMode = string.IsNullOrWhiteSpace(mode) ? "Mois" : mode;
    }

    private void SelectOperationType(string? type)
    {
        SelectedOperationType = string.IsNullOrWhiteSpace(type) ? "Toutes" : type;
    }

    private void ToggleCategory(CategoryFilterItemDto? category)
    {
        if (category is null)
            return;

        int index = Categories.IndexOf(category);
        if (index < 0)
            return;

        Categories[index] = category.WithSelected(!category.IsSelected);
        OnPropertyChanged(nameof(SelectAllCategoriesText));
        RecalculateActiveFilters();
    }

    private void ToggleSelectAllCategories()
    {
        bool selectAll = !Categories.All(category => category.IsSelected);

        for (int i = 0; i < Categories.Count; i++)
            Categories[i] = Categories[i].WithSelected(selectAll);

        OnPropertyChanged(nameof(SelectAllCategoriesText));
        RecalculateActiveFilters();
    }

    private bool Validate()
    {
        HasValidationError = false;
        ValidationErrorMessage = string.Empty;

        if (MinAmount < 0 || MaxAmount < 0)
            return SetValidationError("Les montants doivent être positifs.");

        if (MinAmount.HasValue && MaxAmount.HasValue && MinAmount.Value > MaxAmount.Value)
            return SetValidationError("Le montant minimum doit être inférieur ou égal au montant maximum.");

        if (IsCustomPeriod && CustomStartDate.HasValue && CustomEndDate.HasValue && CustomStartDate.Value.Date > CustomEndDate.Value.Date)
            return SetValidationError("La date de début doit être antérieure à la date de fin.");

        return true;
    }

    private ExpenseFilterDto BuildFilter()
    {
        (DateTime startDate, DateTime endDate) = ResolvePeriod();
        List<int> selectedCategoryIds = Categories
            .Where(category => category.IsSelected)
            .Select(category => category.CategoryId)
            .ToList();

        bool allCategoriesSelected = selectedCategoryIds.Count == Categories.Count;

        return new ExpenseFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
            OperationType = SelectedOperationType,
            CategoryIds = allCategoriesSelected ? [] : selectedCategoryIds,
            MinAmount = MinAmount,
            MaxAmount = MaxAmount,
            PaymentMethod = SelectedPaymentMethod,
            IsFixedCharge = FixedChargesOnly ? true : null,
            SortBy = Filter.SortBy,
            SortDescending = Filter.SortDescending
        };
    }

    private async Task LoadCategoriesAsync()
    {
        var categoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);
        Categories.Clear();

        foreach (var category in (categoriesResult.Data ?? []).Where(category => category.IsActive).OrderBy(category => category.DisplayOrder).ThenBy(category => category.Name))
        {
            bool selected = Filter.CategoryIds.Count == 0 || Filter.CategoryIds.Contains(category.Id);
            Categories.Add(new CategoryFilterItemDto
            {
                CategoryId = category.Id,
                Label = category.Name,
                Icon = string.IsNullOrWhiteSpace(category.Icon) ? "💰" : category.Icon,
                ColorHex = string.IsNullOrWhiteSpace(category.Color) ? "#6793AE" : category.Color,
                IsSelected = selected
            });
        }

        OnPropertyChanged(nameof(SelectAllCategoriesText));
    }

    private void SynchronizeCategorySelection()
    {
        for (int i = 0; i < Categories.Count; i++)
        {
            CategoryFilterItemDto category = Categories[i];
            bool selected = Filter.CategoryIds.Count == 0 || Filter.CategoryIds.Contains(category.CategoryId);
            Categories[i] = category.WithSelected(selected);
        }

        OnPropertyChanged(nameof(SelectAllCategoriesText));
    }

    private static ExpenseFilterDto NormalizeFilter(ExpenseFilterDto? filter)
    {
        ExpenseFilterDto normalized = filter?.Clone() ?? new ExpenseFilterDto();
        normalized.OperationType = string.IsNullOrWhiteSpace(normalized.OperationType) ? "Toutes" : normalized.OperationType;
        normalized.PaymentMethod = string.IsNullOrWhiteSpace(normalized.PaymentMethod) ? "Tous" : normalized.PaymentMethod;
        normalized.SearchText ??= string.Empty;
        normalized.CategoryIds ??= [];
        return normalized;
    }

    private string ResolvePeriodMode(ExpenseFilterDto filter)
    {
        if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
            return "Mois";

        DateTime startDate = filter.StartDate.Value.Date;
        DateTime endDate = filter.EndDate.Value.Date;
        DateTime monthStart = new(startDate.Year, startDate.Month, 1);
        DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

        if (startDate == monthStart && endDate == monthEnd)
            return "Mois";

        if (startDate.Month == 1 && startDate.Day == 1 && endDate.Month == 12 && endDate.Day == 31 && startDate.Year == endDate.Year)
            return "Année";

        int startQuarter = ((startDate.Month - 1) / 3) + 1;
        DateTime quarterStart = new(startDate.Year, ((startQuarter - 1) * 3) + 1, 1);
        DateTime quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
        if (startDate == quarterStart && endDate == quarterEnd)
            return "Trimestre";

        return "Personnalisée";
    }

    private (DateTime StartDate, DateTime EndDate) ResolvePeriod()
    {
        if (IsCustomPeriod)
        {
            DateTime start = CustomStartDate?.Date ?? SelectedMonth;
            DateTime end = CustomEndDate?.Date ?? start;
            return (start, end);
        }

        if (IsYearPeriod)
            return (new DateTime(SelectedYear, 1, 1), new DateTime(SelectedYear, 12, 31));

        if (IsQuarterPeriod)
        {
            int quarter = int.TryParse(SelectedQuarter.AsSpan(1, 1), out int parsedQuarter) ? parsedQuarter : 1;
            int month = ((quarter - 1) * 3) + 1;
            DateTime start = new(SelectedYear, month, 1);
            return (start, start.AddMonths(3).AddDays(-1));
        }

        return (SelectedMonth, SelectedMonth.AddMonths(1).AddDays(-1));
    }

    private void RecalculateActiveFilters()
    {
        ActiveFiltersCount = CountActiveFilters(BuildFilter());
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
        if (!string.Equals(filter.PaymentMethod, "Tous", StringComparison.OrdinalIgnoreCase))
            count++;
        if (filter.IsFixedCharge == true)
            count++;

        return count;
    }

    private bool SetValidationError(string message)
    {
        HasValidationError = true;
        ValidationErrorMessage = message;
        return false;
    }

    private void NotifyOperationSegments()
    {
        OnPropertyChanged(nameof(AllSegmentBackground));
        OnPropertyChanged(nameof(ExpenseSegmentBackground));
        OnPropertyChanged(nameof(IncomeSegmentBackground));
        OnPropertyChanged(nameof(TransferSegmentBackground));
        OnPropertyChanged(nameof(AllSegmentTextColor));
        OnPropertyChanged(nameof(ExpenseSegmentTextColor));
        OnPropertyChanged(nameof(IncomeSegmentTextColor));
        OnPropertyChanged(nameof(TransferSegmentTextColor));
    }

    private void NotifyPeriodSegments()
    {
        OnPropertyChanged(nameof(MonthPeriodBackground));
        OnPropertyChanged(nameof(QuarterPeriodBackground));
        OnPropertyChanged(nameof(YearPeriodBackground));
        OnPropertyChanged(nameof(CustomPeriodBackground));
        OnPropertyChanged(nameof(MonthPeriodTextColor));
        OnPropertyChanged(nameof(QuarterPeriodTextColor));
        OnPropertyChanged(nameof(YearPeriodTextColor));
        OnPropertyChanged(nameof(CustomPeriodTextColor));
    }

    private string SegmentBackground(string segment)
        => string.Equals(SelectedOperationType, segment, StringComparison.OrdinalIgnoreCase) ? "#6793AE" : "#FFFFFF";

    private string SegmentTextColor(string segment)
        => string.Equals(SelectedOperationType, segment, StringComparison.OrdinalIgnoreCase) ? "#FFFFFF" : "#222222";

    private string PeriodBackground(string mode)
        => string.Equals(SelectedPeriodMode, mode, StringComparison.OrdinalIgnoreCase) ? "#6793AE" : "#FFFFFF";

    private string PeriodTextColor(string mode)
        => string.Equals(SelectedPeriodMode, mode, StringComparison.OrdinalIgnoreCase) ? "#FFFFFF" : "#222222";
}

internal static class CategoryFilterItemDtoExtensions
{
    public static CategoryFilterItemDto WithSelected(this CategoryFilterItemDto item, bool isSelected)
        => new()
        {
            CategoryId = item.CategoryId,
            Label = item.Label,
            Icon = item.Icon,
            ColorHex = item.ColorHex,
            IsSelected = isSelected
        };
}
