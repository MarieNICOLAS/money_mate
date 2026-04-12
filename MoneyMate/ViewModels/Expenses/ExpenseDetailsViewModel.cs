using Microsoft.Maui.Graphics;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using System.Windows.Input;

namespace MoneyMate.ViewModels.Expenses;

/// <summary>
/// ViewModel de détail d'une dépense.
/// </summary>
public class ExpenseDetailsViewModel : AuthenticatedViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private int _expenseId;
    private string _amountDisplay = string.Empty;
    private string _categoryName = string.Empty;
    private string _categoryIcon = "💰";
    private Color _categoryColor = Color.FromArgb("#6B7A8F");
    private string _note = string.Empty;
    private DateTime _dateOperation = DateTime.Now;
    private bool _isFixedCharge;

    public ExpenseDetailsViewModel(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        Title = "Détail dépense";
        EditCommand = new Command(async () => await EditAsync());
        DeleteCommand = new Command(async () => await DeleteAsync());
        DuplicateCommand = new Command(async () => await DuplicateAsync());
        BackCommand = new Command(async () => await NavigationService.GoBackAsync());
    }

    public int ExpenseId
    {
        get => _expenseId;
        private set => SetProperty(ref _expenseId, value);
    }

    public string AmountDisplay
    {
        get => _amountDisplay;
        private set => SetProperty(ref _amountDisplay, value);
    }

    public string CategoryName
    {
        get => _categoryName;
        private set => SetProperty(ref _categoryName, value);
    }

    public string CategoryIcon
    {
        get => _categoryIcon;
        private set => SetProperty(ref _categoryIcon, value);
    }

    public Color CategoryColor
    {
        get => _categoryColor;
        private set => SetProperty(ref _categoryColor, value);
    }

    public string Note
    {
        get => _note;
        private set => SetProperty(ref _note, value);
    }

    public DateTime DateOperation
    {
        get => _dateOperation;
        private set => SetProperty(ref _dateOperation, value);
    }

    public bool IsFixedCharge
    {
        get => _isFixedCharge;
        private set => SetProperty(ref _isFixedCharge, value);
    }

    public bool HasExpense => ExpenseId > 0;

    public ICommand EditCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand DuplicateCommand { get; }

    public ICommand BackCommand { get; }

    public async Task InitializeAsync(Dictionary<string, object>? parameters = null)
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            if (!TryGetExpenseId(parameters, out int expenseId))
            {
                ErrorMessage = "Identifiant de dépense manquant.";
                return;
            }

            ExpenseId = expenseId;
            await LoadCoreAsync();
        }, "Une erreur est survenue lors du chargement du détail de la dépense.");
    }

    public async Task LoadAsync()
        => await ExecuteBusyActionAsync(async () => await LoadCoreAsync(), "Une erreur est survenue lors du chargement du détail de la dépense.");

    private async Task LoadCoreAsync()
    {
        if (!EnsureCurrentUser())
            return;

        if (ExpenseId <= 0)
        {
            ErrorMessage = "Identifiant de dépense manquant.";
            return;
        }

        var expenseResult = await _expenseService.GetExpenseByIdAsync(ExpenseId, CurrentUserId);
        if (!expenseResult.IsSuccess || expenseResult.Data == null)
        {
            ErrorMessage = expenseResult.Message;
            return;
        }

        Expense expense = expenseResult.Data;
        var categoriesResult = await _categoryService.GetCategoriesAsync(CurrentUserId);
        Category? category = (categoriesResult.Data ?? []).FirstOrDefault(item => item.Id == expense.CategoryId);

        AmountDisplay = CurrencyHelper.Format(expense.Amount, CurrentDevise);
        CategoryName = category?.Name ?? "Catégorie inconnue";
        CategoryIcon = string.IsNullOrWhiteSpace(category?.Icon) ? "💰" : category!.Icon;
        CategoryColor = TryGetColor(category?.Color);
        Note = string.IsNullOrWhiteSpace(expense.Note) ? "Sans note" : expense.Note;
        DateOperation = expense.DateOperation;
        IsFixedCharge = expense.IsFixedCharge;
        OnPropertyChanged(nameof(HasExpense));
    }

    private async Task EditAsync()
    {
        if (ExpenseId <= 0)
            return;

        await NavigationService.NavigateToAsync("//EditExpensePage", new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = ExpenseId
        });
    }

    private async Task DeleteAsync()
    {
        if (ExpenseId <= 0)
            return;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Suppression",
            "Supprimer cette dépense ?",
            "Oui",
            "Non");

        if (!confirm)
            return;

        await ExecuteBusyActionAsync(async () =>
        {
            var result = await _expenseService.DeleteExpenseAsync(ExpenseId, CurrentUserId);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return;
            }

            await NavigationService.NavigateToAsync("//ExpensesListPage");
        }, "Une erreur est survenue lors de la suppression de la dépense.");
    }

    private async Task DuplicateAsync()
    {
        if (ExpenseId <= 0)
            return;

        await ExecuteBusyActionAsync(async () =>
        {
            var result = await _expenseService.DuplicateExpenseAsync(ExpenseId, CurrentUserId, DateTime.Now);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return;
            }

            await NavigationService.NavigateToAsync("//ExpensesListPage");
        }, "Une erreur est survenue lors de la duplication de la dépense.");
    }

    private static bool TryGetExpenseId(Dictionary<string, object>? parameters, out int expenseId)
        => Forms.FormViewModelBase.TryGetEntityId(parameters, NavigationParameterKeys.ExpenseId, out expenseId);

    private static Color TryGetColor(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                return Color.FromArgb(hex);
            }
            catch
            {
            }
        }

        return Color.FromArgb("#6B7A8F");
    }
}
