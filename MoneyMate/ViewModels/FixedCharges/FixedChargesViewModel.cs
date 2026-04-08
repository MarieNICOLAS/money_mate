using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.FixedCharges;

/// <summary>
/// ViewModel de consultation des charges fixes.
/// </summary>
public class FixedChargesViewModel : AuthenticatedViewModelBase
{
    private readonly IFixedChargeService _fixedChargeService;
    private readonly ICategoryService _categoryService;
    private decimal _projectedMonthlyAmount;

    public FixedChargesViewModel(
        IFixedChargeService fixedChargeService,
        ICategoryService categoryService,
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        _fixedChargeService = fixedChargeService ?? throw new ArgumentNullException(nameof(fixedChargeService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        Title = "Charges fixes";
        FixedCharges = [];

        RefreshCommand = new Command(async () => await LoadAsync());
        GenerateExpensesCommand = new Command(async () => await GenerateExpensesAsync());
        ToggleFixedChargeActiveCommand = new Command<FixedChargeItemViewModel>(async fixedCharge => await ToggleFixedChargeActiveAsync(fixedCharge));
    }

    public ObservableCollection<FixedChargeItemViewModel> FixedCharges { get; }

    public ICommand RefreshCommand { get; }

    public ICommand GenerateExpensesCommand { get; }

    public ICommand ToggleFixedChargeActiveCommand { get; }

    public int ActiveFixedChargesCount => FixedCharges.Count(charge => charge.IsActive);

    public int UpcomingFixedChargesCount => FixedCharges.Count(charge => charge.IsUpcoming);

    public bool HasFixedCharges => FixedCharges.Count > 0;

    public string Devise => CurrentDevise;

    public decimal ProjectedMonthlyAmount
    {
        get => _projectedMonthlyAmount;
        private set => SetProperty(ref _projectedMonthlyAmount, value);
    }

    public async Task LoadAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            var fixedChargesResult = await _fixedChargeService.GetFixedChargesAsync(CurrentUserId);
            if (!fixedChargesResult.IsSuccess)
            {
                ErrorMessage = fixedChargesResult.Message;
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

            List<FixedChargeItemViewModel> items = (fixedChargesResult.Data ?? [])
                .OrderBy(charge => charge.DayOfMonth)
                .ThenBy(charge => charge.Name)
                .Select(charge => FixedChargeItemViewModel.FromModel(charge, categoriesById, Devise))
                .ToList();

            FixedCharges.Clear();
            foreach (FixedChargeItemViewModel item in items)
                FixedCharges.Add(item);

            ProjectedMonthlyAmount = items.Sum(item => item.MonthlyEquivalentAmount);
            RefreshState();
        }, "Une erreur est survenue lors du chargement des charges fixes.");
    }

    private async Task GenerateExpensesAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            DateTime generationLimit = DateTime.Now.Date.AddMonths(1);
            var result = await _fixedChargeService.GenerateExpensesUntilAsync(CurrentUserId, generationLimit);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return;
            }

            await DialogService.ShowAlertAsync(
                "Charges fixes",
                $"{result.Data?.Count ?? 0} dépense(s) récurrente(s) générée(s).",
                "OK");

            await LoadAsync();
        }, "Une erreur est survenue lors de la génération des dépenses récurrentes.");
    }

    private async Task ToggleFixedChargeActiveAsync(FixedChargeItemViewModel? fixedCharge)
    {
        if (fixedCharge == null)
            return;

        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
                return;

            var result = await _fixedChargeService.SetFixedChargeActiveStateAsync(fixedCharge.Id, CurrentUserId, !fixedCharge.IsActive);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return;
            }

            await LoadAsync();
        }, "Une erreur est survenue lors de la mise à jour de la charge fixe.");
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(ActiveFixedChargesCount));
        OnPropertyChanged(nameof(UpcomingFixedChargesCount));
        OnPropertyChanged(nameof(HasFixedCharges));
        OnPropertyChanged(nameof(Devise));
    }
}

/// <summary>
/// Représentation UI d'une charge fixe.
/// </summary>
public sealed class FixedChargeItemViewModel
{
    private static readonly Color DefaultColor = Color.FromArgb("#6B7A8F");

    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public decimal MonthlyEquivalentAmount { get; init; }

    public string FrequencyLabel { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string CategoryIcon { get; init; } = "💰";

    public Color CategoryColor { get; init; } = DefaultColor;

    public int DayOfMonth { get; init; }

    public DateTime NextOccurrenceDate { get; init; }

    public bool IsActive { get; init; }

    public bool AutoCreateExpense { get; init; }

    public string Devise { get; init; } = "EUR";

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public bool IsUpcoming => NextOccurrenceDate.Date <= DateTime.Now.Date.AddDays(30);

    public string ToggleActionText => IsActive ? "Désactiver" : "Activer";

    public static FixedChargeItemViewModel FromModel(FixedCharge fixedCharge, IReadOnlyDictionary<int, Category> categoriesById, string devise)
    {
        ArgumentNullException.ThrowIfNull(fixedCharge);

        categoriesById.TryGetValue(fixedCharge.CategoryId, out Category? category);

        return new FixedChargeItemViewModel
        {
            Id = fixedCharge.Id,
            Name = fixedCharge.Name,
            Description = fixedCharge.Description,
            Amount = fixedCharge.Amount,
            MonthlyEquivalentAmount = GetMonthlyEquivalentAmount(fixedCharge),
            FrequencyLabel = GetFrequencyLabel(fixedCharge.Frequency),
            CategoryName = category?.Name ?? "Catégorie inconnue",
            CategoryIcon = string.IsNullOrWhiteSpace(category?.Icon) ? "💰" : category!.Icon,
            CategoryColor = TryCreateColor(category?.Color),
            DayOfMonth = fixedCharge.DayOfMonth,
            NextOccurrenceDate = fixedCharge.GetNextOccurrenceDate(),
            IsActive = fixedCharge.IsActive,
            AutoCreateExpense = fixedCharge.AutoCreateExpense,
            Devise = devise
        };
    }

    private static decimal GetMonthlyEquivalentAmount(FixedCharge fixedCharge)
        => fixedCharge.Frequency switch
        {
            "Quarterly" => Math.Round(fixedCharge.Amount / 3m, 2),
            "Yearly" => Math.Round(fixedCharge.Amount / 12m, 2),
            _ => fixedCharge.Amount
        };

    private static string GetFrequencyLabel(string frequency)
        => frequency switch
        {
            "Quarterly" => "Trimestrielle",
            "Yearly" => "Annuelle",
            _ => "Mensuelle"
        };

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
