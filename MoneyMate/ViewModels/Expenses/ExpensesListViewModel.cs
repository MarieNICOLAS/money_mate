using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MoneyMate.Configuration;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Expenses;

/// <summary>
/// ViewModel de la liste des dépenses.
/// </summary>
public class ExpensesListViewModel : AuthenticatedViewModelBase
{
    private const string BudgetRequiredMessage = "Créez d’abord un budget avant d’ajouter une dépense.";
    private const AppDataChangeKind RefreshChangeKinds = AppDataChangeKind.Expenses | AppDataChangeKind.Categories;

    private readonly IExpenseService _expenseService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;
    private readonly IAppEventBus _appEventBus;
    private decimal _totalExpenses;
    private long _lastRefreshVersion = -1;

    public ExpensesListViewModel(
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

        Title = "Mes dépenses";
        Expenses = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        AddExpenseCommand = new Command(async () => await NavigateToExpenseCreationAsync(AppRoutes.AddExpense));
        QuickAddExpenseCommand = new Command(async () => await NavigateToExpenseCreationAsync(AppRoutes.QuickAddExpense));
        OpenExpenseDetailsCommand = new Command<ExpenseListItemViewModel>(async expense => await OpenExpenseDetailsAsync(expense));
    }

    public ObservableCollection<ExpenseListItemViewModel> Expenses { get; }

    public ICommand RefreshCommand { get; }

    public ICommand AddExpenseCommand { get; }

    public ICommand QuickAddExpenseCommand { get; }

    public ICommand OpenExpenseDetailsCommand { get; }

    public decimal TotalExpenses
    {
        get => _totalExpenses;
        private set => SetProperty(ref _totalExpenses, value);
    }

    public int ExpensesCount => Expenses.Count;

    public bool HasExpenses => Expenses.Count > 0;

    public string Devise => CurrentDevise;

    public async Task LoadAsync()
    {
        await ExecuteBusyActionAsync(LoadCoreAsync, "Une erreur est survenue lors du chargement des dépenses.");
    }

    public async Task RefreshIfNeededAsync()
    {
        if (_lastRefreshVersion < 0 || _appEventBus.HasChangedSince(RefreshChangeKinds, _lastRefreshVersion))
            await LoadAsync();
    }

    private async Task LoadCoreAsync()
    {
        if (!EnsureCurrentUser())
            return;

        var expensesResult = await _expenseService.GetExpensesAsync(CurrentUserId);
        if (!expensesResult.IsSuccess)
        {
            ErrorMessage = expensesResult.Message;
            RefreshState();
            return;
        }

        var categoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);
        if (!categoriesResult.IsSuccess)
        {
            ErrorMessage = categoriesResult.Message;
            RefreshState();
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
        {
            ExpenseListItemViewModel item = ExpenseListItemViewModel.FromModel(expense, categoriesById, Devise);
            item.OpenCommand = OpenExpenseDetailsCommand;
            Expenses.Add(item);
        }

        TotalExpenses = expenses.Sum(expense => expense.Amount);
        RefreshState();
        UpdateObservedRefreshVersion();
    }

    private void UpdateObservedRefreshVersion()
        => _lastRefreshVersion = _appEventBus.GetVersion(RefreshChangeKinds);

    private void RefreshState()
    {
        OnPropertyChanged(nameof(ExpensesCount));
        OnPropertyChanged(nameof(HasExpenses));
        OnPropertyChanged(nameof(Devise));
    }

    private async Task OpenExpenseDetailsAsync(ExpenseListItemViewModel? expense)
    {
        if (expense == null)
            return;

        await NavigationService.NavigateToAsync(AppRoutes.ExpenseDetails, new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = expense.Id
        });
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
}

/// <summary>
/// Représentation UI d'une dépense.
/// </summary>
public class ExpenseListItemViewModel
{
    private static readonly Color DefaultColor = Color.FromArgb("#6B7A8F");

    public int Id { get; init; }

    public decimal Amount { get; init; }

    public DateTime ExpenseDate { get; init; }

    public string Note { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryIcon { get; init; } = "💰";

    public Color CategoryColor { get; init; } = DefaultColor;

    public string Devise { get; init; } = "EUR";

    public ICommand? OpenCommand { get; set; }

    public string AmountDisplay => CurrencyHelper.Format(Amount, Devise);

    public string DateDisplay => ExpenseDate.ToString("dd/MM/yyyy");

    public static ExpenseListItemViewModel FromModel(Expense expense, IReadOnlyDictionary<int, Category> categoriesById, string devise)
    {
        ArgumentNullException.ThrowIfNull(expense);

        categoriesById ??= new Dictionary<int, Category>();
        categoriesById.TryGetValue(expense.CategoryId, out Category? category);

        string categoryName = string.IsNullOrWhiteSpace(category?.Name)
            ? "Catégorie inconnue"
            : category!.Name.Trim();

        string note = string.IsNullOrWhiteSpace(expense.Note)
            ? (expense.IsFixedCharge ? "Charge fixe" : "Sans note")
            : expense.Note.Trim();

        string displayName = string.IsNullOrWhiteSpace(expense.Note)
            ? categoryName
            : expense.Note.Trim();

        return new ExpenseListItemViewModel
        {
            Id = expense.Id,
            Amount = expense.Amount,
            ExpenseDate = expense.DateOperation,
            Note = note,
            DisplayName = displayName,
            CategoryName = categoryName,
            CategoryIcon = string.IsNullOrWhiteSpace(category?.Icon) ? "💰" : category!.Icon,
            CategoryColor = TryCreateColor(category?.Color),
            Devise = string.IsNullOrWhiteSpace(devise) ? "EUR" : devise.Trim().ToUpperInvariant()
        };
    }

    private static Color TryCreateColor(string? color)
    {
        if (!string.IsNullOrWhiteSpace(color))
        {
            try
            {
                return Color.FromArgb(color);
            }
            catch
            {
            }
        }

        return DefaultColor;
    }
}

public sealed class ExpenseItemViewModel : ExpenseListItemViewModel
{
}
