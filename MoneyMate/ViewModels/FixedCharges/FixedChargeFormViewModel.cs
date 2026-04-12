using System.Collections.ObjectModel;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.ViewModels.Forms;

namespace MoneyMate.ViewModels.FixedCharges;

/// <summary>
/// Formulaire de création / édition d'une charge fixe.
/// </summary>
public class FixedChargeFormViewModel : FormViewModelBase
{
    private readonly IFixedChargeService _fixedChargeService;
    private readonly ICategoryService _categoryService;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _amountText = string.Empty;
    private int _selectedCategoryId;
    private string _selectedFrequency = "Monthly";
    private string _dayOfMonthText = "1";
    private DateTime _startDate = DateTime.Today;
    private bool _hasEndDate;
    private DateTime _endDate = DateTime.Today;
    private bool _isActive = true;
    private bool _autoCreateExpense = true;
    private DateTime _createdAt = DateTime.UtcNow;

    public FixedChargeFormViewModel(
        IFixedChargeService fixedChargeService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _fixedChargeService = fixedChargeService ?? throw new ArgumentNullException(nameof(fixedChargeService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        Categories = [];
        Frequencies = ["Monthly", "Quarterly", "Yearly"];
        Title = "Charge fixe";
        RefreshFormState();
    }

    public ObservableCollection<CategoryOptionViewModel> Categories { get; }

    public IReadOnlyList<string> Frequencies { get; }

    public string Name
    {
        get => _name;
        set => SetFormProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetFormProperty(ref _description, value);
    }

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

    public string SelectedFrequency
    {
        get => _selectedFrequency;
        set => SetFormProperty(ref _selectedFrequency, value);
    }

    public string DayOfMonthText
    {
        get => _dayOfMonthText;
        set => SetFormProperty(ref _dayOfMonthText, value);
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

    public bool AutoCreateExpense
    {
        get => _autoCreateExpense;
        set => SetFormProperty(ref _autoCreateExpense, value);
    }

    protected override string EditParameterKey => NavigationParameterKeys.FixedChargeId;

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
        Title = "Nouvelle charge fixe";
        Name = string.Empty;
        Description = string.Empty;
        AmountText = string.Empty;
        SelectedCategoryId = Categories.FirstOrDefault()?.Id ?? 0;
        SelectedFrequency = "Monthly";
        DayOfMonthText = "1";
        StartDate = DateTime.Today;
        HasEndDate = false;
        EndDate = DateTime.Today;
        IsActive = true;
        AutoCreateExpense = true;
        _createdAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    protected override async Task LoadForEditAsync(int entityId)
    {
        var result = await _fixedChargeService.GetFixedChargeByIdAsync(entityId, CurrentUserId);
        if (!result.IsSuccess || result.Data == null)
        {
            ErrorMessage = result.Message;
            return;
        }

        FixedCharge fixedCharge = result.Data;
        Title = "Modifier la charge fixe";
        Name = fixedCharge.Name;
        Description = fixedCharge.Description;
        AmountText = fixedCharge.Amount.ToString("0.##");
        SelectedCategoryId = fixedCharge.CategoryId;
        SelectedFrequency = fixedCharge.Frequency;
        DayOfMonthText = fixedCharge.DayOfMonth.ToString();
        StartDate = fixedCharge.StartDate;
        HasEndDate = fixedCharge.EndDate.HasValue;
        EndDate = fixedCharge.EndDate ?? fixedCharge.StartDate;
        IsActive = fixedCharge.IsActive;
        AutoCreateExpense = fixedCharge.AutoCreateExpense;
        _createdAt = fixedCharge.CreatedAt;
    }

    protected override string ValidateForm()
    {
        if (CurrentUserId <= 0)
            return "Aucune session utilisateur active.";

        if (string.IsNullOrWhiteSpace(Name))
            return "Le nom de la charge fixe est requis.";

        if (!TryParseDecimalInput(AmountText, out decimal amount) || amount <= 0)
            return "Le montant doit être strictement positif.";

        if (SelectedCategoryId <= 0)
            return "La catégorie est requise.";

        if (string.IsNullOrWhiteSpace(SelectedFrequency) || !Frequencies.Contains(SelectedFrequency))
            return "La fréquence est invalide.";

        if (!int.TryParse(DayOfMonthText, out int dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31)
            return "Le jour du mois doit être compris entre 1 et 31.";

        if (HasEndDate && StartDate > EndDate)
            return "La période de la charge fixe est invalide.";

        return string.Empty;
    }

    protected override async Task<bool> SaveCoreAsync()
    {
        _ = TryParseDecimalInput(AmountText, out decimal amount);

        FixedCharge fixedCharge = new()
        {
            Id = EditingEntityId,
            UserId = CurrentUserId,
            Name = Name.Trim(),
            Description = Description.Trim(),
            Amount = amount,
            CategoryId = SelectedCategoryId,
            Frequency = SelectedFrequency,
            DayOfMonth = int.Parse(DayOfMonthText),
            StartDate = StartDate,
            EndDate = HasEndDate ? EndDate : null,
            IsActive = IsActive,
            AutoCreateExpense = AutoCreateExpense,
            CreatedAt = _createdAt
        };

        var result = IsEditMode
            ? await _fixedChargeService.UpdateFixedChargeAsync(fixedCharge)
            : await _fixedChargeService.CreateFixedChargeAsync(fixedCharge);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync("//FixedChargesPage");
        return true;
    }

    protected override async Task<bool> DeleteCoreAsync()
    {
        if (!IsEditMode)
            return false;

        bool confirm = await DialogService.ShowConfirmationAsync(
            "Suppression",
            "Supprimer cette charge fixe ?",
            "Oui",
            "Non");

        if (!confirm)
            return false;

        var result = await _fixedChargeService.DeleteFixedChargeAsync(EditingEntityId, CurrentUserId);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message;
            return false;
        }

        await NavigationService.NavigateToAsync("//FixedChargesPage");
        return true;
    }
}
