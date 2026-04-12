using System.Collections.ObjectModel;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;

namespace MoneyMate.ViewModels.Expenses;

/// <summary>
/// Formulaire d'ajout rapide d'une dépense.
/// </summary>
public class QuickAddExpenseViewModel : FormViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly ICategoryService _categoryService;
    private string _amountText = string.Empty;
    private int _selectedCategoryId;
    private CategoryOptionViewModel? _selectedCategory;
    private string _note = string.Empty;

    public QuickAddExpenseViewModel(
        IExpenseService expenseService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
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

    protected override string EditParameterKey => NavigationParameterKeys.ExpenseId;

    protected override bool CanDeleteEntity => false;

    protected override async Task LoadLookupsAsync()
    {
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

        if (!TryParseDecimalInput(AmountText, out decimal amount) || amount <= 0)
            return "Le montant doit être strictement positif.";

        if (SelectedCategoryId <= 0)
            return "La catégorie est requise.";

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

        await NavigationService.NavigateToAsync("//ExpensesListPage");
        return true;
    }
}
