using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Expenses;

/// <summary>
/// ViewModel de la liste des dépenses.
/// </summary>
public class ExpensesListViewModel : AuthenticatedViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private decimal _totalExpenses;

    public ExpensesListViewModel(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        Title = "Mes dépenses";
        Expenses = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        AddExpenseCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.AddExpense));
        QuickAddExpenseCommand = new Command(async () => await NavigationService.NavigateToAsync(AppRoutes.QuickAddExpense));
        OpenExpenseDetailsCommand = new Command<ExpenseItemViewModel>(async expense => await OpenExpenseDetailsAsync(expense));
    }

    public ObservableCollection<ExpenseItemViewModel> Expenses { get; }

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
        await ExecuteBusyActionAsync(async () =>
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
                Expenses.Add(ExpenseItemViewModel.FromModel(expense, categoriesById, Devise));

            TotalExpenses = expenses.Sum(expense => expense.Amount);
            RefreshState();
        }, "Une erreur est survenue lors du chargement des dépenses.");
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(ExpensesCount));
        OnPropertyChanged(nameof(HasExpenses));
        OnPropertyChanged(nameof(Devise));
    }

    private async Task OpenExpenseDetailsAsync(ExpenseItemViewModel? expense)
    {
        if (expense == null)
            return;

        await NavigationService.NavigateToAsync(AppRoutes.ExpenseDetails, new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = expense.Id
        });
    }
}

/// <summary>
/// Représentation UI d'une dépense.
/// </summary>
public sealed class ExpenseItemViewModel
{
    private static readonly Color DefaultColor = Color.FromArgb("#6B7A8F");

    public int Id { get; init; }

    public decimal Amount { get; init; }

    public DateTime ExpenseDate { get; init; }

    public string Note { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryIcon { get; init; } = "💰";

    public Color CategoryColor { get; init; } = DefaultColor;

    public string Devise { get; init; } = "EUR";

    public static ExpenseItemViewModel FromModel(Expense expense, IReadOnlyDictionary<int, Category> categoriesById, string devise)
    {
        ArgumentNullException.ThrowIfNull(expense);

        categoriesById.TryGetValue(expense.CategoryId, out Category? category);

        return new ExpenseItemViewModel
        {
            Id = expense.Id,
            Amount = expense.Amount,
            ExpenseDate = expense.DateOperation,
            Note = string.IsNullOrWhiteSpace(expense.Note)
                ? (expense.IsFixedCharge ? "Charge fixe" : "Sans note")
                : expense.Note,
            CategoryName = category?.Name ?? "Catégorie inconnue",
            CategoryIcon = string.IsNullOrWhiteSpace(category?.Icon) ? "💰" : category!.Icon,
            CategoryColor = TryCreateColor(category?.Color),
            Devise = devise
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
