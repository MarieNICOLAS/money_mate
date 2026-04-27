using MoneyMate.Models;
using MoneyMate.Models.DTOs;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.Services.Implementations;

public sealed class CalendarService : ICalendarService
{
    private readonly IExpenseService _expenseService;
    private readonly IFixedChargeService _fixedChargeService;
    private readonly ICategoryService _categoryService;

    public CalendarService(
        IExpenseService expenseService,
        IFixedChargeService fixedChargeService,
        ICategoryService categoryService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
        _fixedChargeService = fixedChargeService ?? throw new ArgumentNullException(nameof(fixedChargeService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    public async Task<IReadOnlyList<CalendarOperationDto>> GetOperationsForMonthAsync(int userId, DateTime month)
    {
        if (userId <= 0)
            return [];

        DateTime monthStart = new(month.Year, month.Month, 1);
        DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var categories = await GetCategoriesByIdAsync(userId);
        var operations = new List<CalendarOperationDto>();

        var expensesResult = await _expenseService.GetExpensesByPeriodAsync(userId, monthStart, monthEnd);
        if (expensesResult.IsSuccess)
            operations.AddRange((expensesResult.Data ?? []).Select(expense => MapExpense(expense, categories)));

        var fixedChargesResult = await _fixedChargeService.GetFixedChargesAsync(userId);
        if (fixedChargesResult.IsSuccess)
        {
            operations.AddRange((fixedChargesResult.Data ?? [])
                .Where(charge => charge.IsActive)
                .SelectMany(charge => BuildFixedChargeOccurrences(charge, categories, monthStart, monthEnd)));
        }

        return operations
            .OrderBy(operation => operation.Date)
            .ThenBy(operation => operation.Type)
            .ThenBy(operation => operation.Title)
            .ToList();
    }

    public async Task<IReadOnlyList<CalendarOperationDto>> GetOperationsForDayAsync(int userId, DateTime date)
    {
        IReadOnlyList<CalendarOperationDto> monthOperations = await GetOperationsForMonthAsync(userId, date);
        return monthOperations
            .Where(operation => operation.Date.Date == date.Date)
            .OrderBy(operation => operation.Type)
            .ThenBy(operation => operation.Title)
            .ToList();
    }

    private async Task<Dictionary<int, Category>> GetCategoriesByIdAsync(int userId)
    {
        var categoriesResult = await _categoryService.GetCategoriesAsync(userId);
        if (!categoriesResult.IsSuccess)
            return [];

        return (categoriesResult.Data ?? [])
            .GroupBy(category => category.Id)
            .Select(group => group.First())
            .ToDictionary(category => category.Id, category => category);
    }

    private static CalendarOperationDto MapExpense(Expense expense, IReadOnlyDictionary<int, Category> categories)
    {
        categories.TryGetValue(expense.CategoryId, out Category? category);

        string type = ResolveOperationType(expense, category);
        bool isIncome = string.Equals(type, "Revenu", StringComparison.OrdinalIgnoreCase);
        bool isTransfer = string.Equals(type, "Transfert", StringComparison.OrdinalIgnoreCase);
        bool isFixedCharge = expense.IsFixedCharge;

        return new CalendarOperationDto
        {
            Id = expense.Id,
            CategoryId = expense.CategoryId,
            Title = string.IsNullOrWhiteSpace(expense.Note) ? category?.Name ?? "Opération" : expense.Note.Trim(),
            CategoryName = category?.Name ?? "Catégorie",
            Icon = string.IsNullOrWhiteSpace(category?.Icon) ? "💰" : category!.Icon,
            Amount = Math.Abs(expense.Amount),
            Date = expense.DateOperation.Date,
            Type = type,
            IsFixedCharge = isFixedCharge,
            StatusLabel = expense.DateOperation.Date <= DateTime.Today ? isIncome ? "Reçu" : "Prélevé" : "À venir",
            AmountColor = isIncome ? "#5CB85C" : isTransfer ? "#6B7A8F" : "#D9534F",
            BadgeBackgroundColor = isIncome ? "#EAF7EA" : isFixedCharge ? "#FFF2EC" : "#FFF0EE"
        };
    }

    private static IEnumerable<CalendarOperationDto> BuildFixedChargeOccurrences(
        FixedCharge charge,
        IReadOnlyDictionary<int, Category> categories,
        DateTime monthStart,
        DateTime monthEnd)
    {
        categories.TryGetValue(charge.CategoryId, out Category? category);

        foreach (DateTime occurrence in EnumerateOccurrencesInMonth(charge, monthStart, monthEnd))
        {
            yield return new CalendarOperationDto
            {
                Id = -charge.Id,
                CategoryId = charge.CategoryId,
                Title = charge.Name,
                CategoryName = category?.Name ?? "Charge fixe",
                Icon = string.IsNullOrWhiteSpace(category?.Icon) ? "📌" : category!.Icon,
                Amount = Math.Abs(charge.Amount),
                Date = occurrence.Date,
                Type = "Dépense",
                IsFixedCharge = true,
                StatusLabel = occurrence.Date <= DateTime.Today ? "Prélevé" : "À venir",
                AmountColor = "#D9534F",
                BadgeBackgroundColor = "#FFF2EC"
            };
        }
    }

    private static IEnumerable<DateTime> EnumerateOccurrencesInMonth(FixedCharge charge, DateTime monthStart, DateTime monthEnd)
    {
        DateTime occurrence = charge.StartDate.Date;

        if (charge.DayOfMonth > 0)
        {
            int day = Math.Min(charge.DayOfMonth, DateTime.DaysInMonth(occurrence.Year, occurrence.Month));
            occurrence = new DateTime(occurrence.Year, occurrence.Month, day);
        }

        while (occurrence <= monthEnd)
        {
            if (charge.EndDate.HasValue && occurrence.Date > charge.EndDate.Value.Date)
                yield break;

            if (occurrence >= monthStart)
                yield return occurrence;

            occurrence = charge.Frequency switch
            {
                "Quarterly" => occurrence.AddMonths(3),
                "Yearly" => occurrence.AddYears(1),
                _ => occurrence.AddMonths(1)
            };
        }
    }

    private static string ResolveOperationType(Expense expense, Category? category)
    {
        string haystack = $"{category?.Name} {expense.Note}".ToLowerInvariant();

        if (expense.Amount < 0 || haystack.Contains("revenu") || haystack.Contains("income") || haystack.Contains("salaire"))
            return "Revenu";

        if (haystack.Contains("transfert") || haystack.Contains("transfer") || haystack.Contains("virement interne"))
            return "Transfert";

        return "Dépense";
    }
}
