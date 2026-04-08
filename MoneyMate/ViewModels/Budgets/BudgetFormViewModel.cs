using System.Collections.ObjectModel;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;

namespace MoneyMate.ViewModels.Budgets;

/// <summary>
/// Formulaire de création / édition d'un budget.
/// </summary>
public class BudgetFormViewModel : FormViewModelBase
{
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;
    private string _amountText = string.Empty;
    private int _selectedCategoryId;
    private string _selectedPeriodType = "Monthly";
    private DateTime _startDate = DateTime.Today;
    private bool _hasEndDate;
    private DateTime _endDate = DateTime.Today;
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;

    public BudgetFormViewModel(
        IBudgetService budgetService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        Categories = [];
        PeriodTypes = ["Weekly", "Monthly", "Yearly"];
        Title = "Budget";
        RefreshFormState();
    }

    public ObservableCollection<CategoryOptionViewModel> Categories { get; }

    public IReadOnlyList<string> PeriodTypes { get; }

    public string AmountText
    {
        get => _amountText;
        set => SetFormProperty(ref _amountText, value);
    }

    public int SelectedCategoryId
    {
        get => _selectedCategoryId;
        set => SetFormProperty(ref _selectedCategoryId, value);
    }

    public string SelectedPeriodType
    {
        get => _selectedPeriodType;
        set => SetFormProperty(ref _selectedPeriodType, value);
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetFormProperty(ref _startDate, value);
    }

    public bool HasEndDate
    {
        get => _hasEndDate;
        set => SetFormProperty(ref _hasEndDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetFormProperty(ref _endDate, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetFormProperty(ref _isActive, value);
    }

    protected override string EditParameterKey => NavigationParameterKeys.BudgetId;

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
    }

    protected override Task InitializeForCreateAsync()
    {
        Title = "Nouveau budget";
        AmountText = string.Empty;
        SelectedCategoryId = Categories.FirstOrDefault()?.Id ?? 0;
        SelectedPeriodType = "Monthly";
        StartDate = DateTime.Today;
        HasEndDate = false;
        EndDate = DateTime.Today;
        IsActive = true;
        _createdAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    protected override async Task LoadForEditAsync(int entityId)
    {
        var result = await _budgetService.GetBudgetByIdAsync(entityId, CurrentUserId);
        if (!result.IsSuccess || result.Data == null)
        {
            ErrorMessage = result.Message;
            return;
        }

        Budget budget = result.Data;
        Title = "Modifier le budget";
        AmountText = budget.Amount.ToString("0.##");
        SelectedCategoryId = budget.CategoryId;
        SelectedPeriodType = budget.PeriodType;
        StartDate = budget.StartDate;
        HasEndDate = budget.EndDate.HasValue;
        EndDate = budget.EndDate ?? budget.StartDate;
        IsActive = budget.IsActive;
        _createdAt = budget.CreatedAt;
    }

    protected override string ValidateForm()
    {
        if (CurrentUserId <= 0)
            return "Aucune session utilisateur active.";

        if (!TryParseDecimalInput(AmountText, out decimal amount) || amount <= 0)
            return "Le montant du budget doit être strictement positif.";

        if (SelectedCategoryId <= 0)
            return "La catégorie est requise.";

        if (string.IsNullOrWhiteSpace(SelectedPeriodType) || !PeriodTypes.Contains(SelectedPeriodType))
            return "Le type de période est invalide.";

        if (HasEndDate && StartDate > EndDate)
            return "La période du budget est invalide.";

        return string.Empty;
    }

    protected override async Task<bool> SaveCoreAsync()
    {
        _ = TryParseDecimalInput(AmountText, out decimal amount);

        Budget budget = new()
        {
            Id = EditingEntityId,
            UserId = CurrentUserId,
            CategoryId = SelectedCategoryId,
            Amount = amount,
            PeriodType = SelectedPeriodType,
            StartDate = StartDate,
            EndDate = HasEndDate ? EndDate : null,
            IsActive = IsActive,
            CreatedAt = _createdAt
        };

        var result = IsEditMode
            ? await _budgetService.UpdateBudgetAsync(budget)
            : await _budgetService.CreateBudgetAsync(budget);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync("//BudgetsOverviewPage");
        return true;
    }

    protected override async Task<bool> DeleteCoreAsync()
    {
        if (!IsEditMode)
            return false;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Suppression",
            "Supprimer ce budget ?",
            "Oui",
            "Non");

        if (!confirm)
            return false;

        var result = await _budgetService.DeleteBudgetAsync(EditingEntityId, CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync("//BudgetsOverviewPage");
        return true;
    }
}
