using System.Collections.ObjectModel;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;

namespace MoneyMate.ViewModels.Expenses;

/// <summary>
/// Formulaire de création / édition d'une dépense.
/// </summary>
public class ExpenseFormViewModel : FormViewModelBase
{
    private const string BudgetRequiredMessage = "Créez d’abord un budget avant d’ajouter une dépense.";

    private readonly IExpenseService _expenseService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;
    private List<Budget> _activeBudgets = [];
    private string _amountText = string.Empty;
    private int _selectedCategoryId;
    private CategoryOptionViewModel? _selectedCategory;
    private DateTime _dateOperation = DateTime.Now;
    private string _note = string.Empty;
    private bool _isFixedCharge;

    public ExpenseFormViewModel(
        IExpenseService expenseService,
        IBudgetService budgetService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        Categories = [];
        Title = "Dépense";
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

    public DateTime DateOperation
    {
        get => _dateOperation;
        set => SetFormProperty(ref _dateOperation, value);
    }

    public string Note
    {
        get => _note;
        set => SetFormProperty(ref _note, value);
    }

    public bool IsFixedCharge
    {
        get => _isFixedCharge;
        set => SetFormProperty(ref _isFixedCharge, value);
    }

    public bool HasAvailableBudget => _activeBudgets.Count > 0;

    public bool ShowBudgetRequirement => !IsEditMode && !HasAvailableBudget;

    public string BudgetRequirementMessage => BudgetRequiredMessage;

    protected override string EditParameterKey => NavigationParameterKeys.ExpenseId;

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
        Title = "Nouvelle dépense";
        AmountText = string.Empty;
        SelectedCategoryId = Categories.FirstOrDefault()?.Id ?? 0;
        DateOperation = DateTime.Now;
        Note = string.Empty;
        IsFixedCharge = false;
        return Task.CompletedTask;
    }

    protected override async Task LoadForEditAsync(int entityId)
    {
        var result = await _expenseService.GetExpenseByIdAsync(entityId, CurrentUserId);
        if (!result.IsSuccess || result.Data == null)
        {
            ErrorMessage = result.Message;
            return;
        }

        Expense expense = result.Data;
        Title = "Modifier la dépense";
        AmountText = expense.Amount.ToString("0.##");
        SelectedCategoryId = expense.CategoryId;
        DateOperation = expense.DateOperation;
        Note = expense.Note;
        IsFixedCharge = expense.IsFixedCharge;
    }

    protected override string ValidateForm()
    {
        if (CurrentUserId <= 0)
            return "Aucune session utilisateur active.";

        if (!IsEditMode && !HasAvailableBudget)
            return BudgetRequiredMessage;

        if (!TryParseDecimalInput(AmountText, out decimal amount) || amount <= 0)
            return "Le montant doit être strictement positif.";

        if (SelectedCategoryId <= 0)
            return "La catégorie est requise.";

        if (DateOperation > DateTime.Now)
            return "La date de la dépense ne peut pas être dans le futur.";

        if (!IsEditMode && !HasBudgetForDate(DateOperation))
            return "Aucun budget actif ne couvre la date sélectionnée.";

        return string.Empty;
    }

    protected override async Task<bool> SaveCoreAsync()
    {
        _ = TryParseDecimalInput(AmountText, out decimal amount);

        Expense expense = new()
        {
            Id = EditingEntityId,
            UserId = CurrentUserId,
            Amount = amount,
            CategoryId = SelectedCategoryId,
            DateOperation = DateOperation,
            Note = Note,
            IsFixedCharge = IsFixedCharge
        };

        var result = IsEditMode
            ? await _expenseService.UpdateExpenseAsync(expense)
            : await _expenseService.CreateExpenseAsync(expense);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync(AppRoutes.ExpensesList);
        return true;
    }

    protected override async Task<bool> DeleteCoreAsync()
    {
        if (!IsEditMode)
            return false;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Suppression",
            "Supprimer cette dépense ?",
            "Oui",
            "Non");

        if (!confirm)
            return false;

        var result = await _expenseService.DeleteExpenseAsync(EditingEntityId, CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync(AppRoutes.ExpensesList);
        return true;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsEditMode))
            OnPropertyChanged(nameof(ShowBudgetRequirement));
    }

    private async Task LoadBudgetsAsync()
    {
        var result = await _budgetService.GetBudgetsAsync(CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            _activeBudgets = [];
            OnPropertyChanged(nameof(HasAvailableBudget));
            OnPropertyChanged(nameof(ShowBudgetRequirement));
            return;
        }

        _activeBudgets = (result.Data ?? [])
            .Where(budget => budget.IsActive)
            .Select(NormalizeBudget)
            .ToList();

        OnPropertyChanged(nameof(HasAvailableBudget));
        OnPropertyChanged(nameof(ShowBudgetRequirement));
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
