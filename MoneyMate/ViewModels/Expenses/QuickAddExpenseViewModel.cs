using System.Collections.ObjectModel;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;

namespace MoneyMate.ViewModels.Expenses;

/// <summary>
/// Formulaire d'ajout rapide d'une dépense.
/// </summary>
public class QuickAddExpenseViewModel : FormViewModelBase
{
    private const string BudgetRequiredMessage = "Créez d’abord un budget avant d’ajouter une dépense.";

    private readonly IExpenseService _expenseService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;
    private readonly IAppEventBus _appEventBus;
    private List<Budget> _activeBudgets = [];
    private string _amountText = string.Empty;
    private int _selectedCategoryId;
    private CategoryOptionViewModel? _selectedCategory;
    private string _note = string.Empty;

    public QuickAddExpenseViewModel(
        IExpenseService expenseService,
        IBudgetService budgetService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService,
        IAppEventBus? appEventBus = null)
        : base(authenticationService, dialogService, navigationService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _appEventBus = appEventBus ?? NullAppEventBus.Instance;
        Categories = [];
        Title = "Ajout rapide";
        RefreshFormState();
    }

    public ObservableCollection<CategoryOptionViewModel> Categories { get; }

    public string AmountText
    {
        get => _amountText;
        set => SetFormProperty(ref _amountText, value);
    }

    public int SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            if (SetFormProperty(ref _selectedCategoryId, value))
            {
                CategoryOptionViewModel? matchingCategory = Categories.FirstOrDefault(category => category.Id == value);
                if (!ReferenceEquals(_selectedCategory, matchingCategory))
                    SetProperty(ref _selectedCategory, matchingCategory, nameof(SelectedCategory));
            }
        }
    }

    public CategoryOptionViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                SelectedCategoryId = value?.Id ?? 0;
        }
    }

    public string Note
    {
        get => _note;
        set => SetFormProperty(ref _note, value);
    }

    public bool HasAvailableBudget => _activeBudgets.Count > 0;

    public string BudgetRequirementMessage => BudgetRequiredMessage;

    public bool HasBudgetRequirementError => ValidationMessage == BudgetRequiredMessage;

    public bool HasFormValidationError => HasValidationErrors && !HasBudgetRequirementError;

    protected override string EditParameterKey => NavigationParameterKeys.ExpenseId;

    protected override bool CanDeleteEntity => false;

    protected override string? CancelNavigationFallbackRoute => AppRoutes.Dashboard;

    protected override async Task LoadLookupsAsync()
    {
        await LoadBudgetsAsync();

        var result = await _categoryService.GetCategoriesAsync(CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            Categories.Clear();
            return;
        }

        Categories.Clear();
        foreach (Category category in (result.Data ?? []).OrderBy(category => category.Name))
            Categories.Add(CategoryOptionViewModel.FromModel(category));

        if (SelectedCategoryId > 0)
            SelectedCategory = Categories.FirstOrDefault(category => category.Id == SelectedCategoryId);
    }

    protected override Task InitializeForCreateAsync()
    {
        Title = "Ajout rapide";
        AmountText = string.Empty;
        SelectedCategoryId = Categories.FirstOrDefault()?.Id ?? 0;
        Note = string.Empty;
        return Task.CompletedTask;
    }

    protected override Task LoadForEditAsync(int entityId)
    {
        return InitializeForCreateAsync();
    }

    protected override string ValidateForm()
    {
        if (CurrentUserId <= 0)
            return "Aucune session utilisateur active.";

        if (!HasAvailableBudget)
            return BudgetRequiredMessage;

        if (!TryParseDecimalInput(AmountText, out decimal amount) || amount <= 0)
            return "Le montant doit être strictement positif.";

        if (SelectedCategoryId <= 0)
            return "La catégorie est requise.";

        if (!HasBudgetForDate(DateTime.Today))
            return "Aucun budget actif ne couvre la date du jour.";

        return string.Empty;
    }

    protected override async Task<bool> SaveCoreAsync()
    {
        _ = TryParseDecimalInput(AmountText, out decimal amount);

        Expense expense = new()
        {
            UserId = CurrentUserId,
            Amount = amount,
            CategoryId = SelectedCategoryId,
            DateOperation = DateTime.Now,
            Note = Note,
            IsFixedCharge = false
        };

        var result = await _expenseService.CreateExpenseAsync(expense);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        _appEventBus.PublishDataChanged(AppDataChangeKind.Expenses);
        await NavigationService.NavigateToAsync(AppRoutes.ExpensesList);
        return true;
    }

    private async Task LoadBudgetsAsync()
    {
        var result = await _budgetService.GetBudgetsAsync(CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            _activeBudgets = [];
            OnPropertyChanged(nameof(HasAvailableBudget));
            RefreshFormState();
            return;
        }

        _activeBudgets = (result.Data ?? [])
            .Where(budget => budget.IsActive)
            .Select(NormalizeBudget)
            .ToList();

        OnPropertyChanged(nameof(HasAvailableBudget));
        RefreshFormState();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(ValidationMessage))
        {
            OnPropertyChanged(nameof(HasBudgetRequirementError));
            OnPropertyChanged(nameof(HasFormValidationError));
        }
    }

    private bool HasBudgetForDate(DateTime date)
    {
        DateTime targetDate = date.Date;

        return _activeBudgets.Any(budget =>
            targetDate >= budget.StartDate.Date &&
            targetDate <= (budget.EndDate ?? DateTime.MaxValue).Date);
    }

    private static Budget NormalizeBudget(Budget budget)
    {
        DateTime startDate = new(budget.StartDate.Year, budget.StartDate.Month, 1);

        return new Budget
        {
            Id = budget.Id,
            UserId = budget.UserId,
            CategoryId = budget.CategoryId,
            Amount = budget.Amount,
            PeriodType = budget.PeriodType,
            StartDate = startDate,
            EndDate = (budget.EndDate ?? startDate.AddMonths(1).AddDays(-1)).Date,
            IsActive = budget.IsActive,
            CreatedAt = budget.CreatedAt
        };
    }
}
