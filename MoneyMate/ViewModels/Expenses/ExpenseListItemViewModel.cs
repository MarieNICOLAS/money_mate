using System.Windows.Input;
using Microsoft.Maui.Graphics;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Models.DTOs;

namespace MoneyMate.ViewModels.Expenses;

public class ExpenseListItemViewModel : ExpenseListItemDto
{
    private static readonly Color DefaultColor = Color.FromArgb("#6B7A8F");

    public DateTime ExpenseDate
    {
        get => OperationDate;
        init => OperationDate = value;
    }

    public string DisplayName
    {
        get => Title;
        init => Title = value;
    }

    public string CategoryIcon
    {
        get => Icon;
        init => Icon = value;
    }

    public Color CategoryColor { get; init; } = DefaultColor;

    public ICommand? OpenCommand { get; set; }

    public string AmountDisplay => CurrencyHelper.Format(Amount, Devise);

    public string DateDisplay => OperationDate.ToString("dd/MM/yyyy");

    public static ExpenseListItemViewModel FromModel(
        Expense expense,
        IReadOnlyDictionary<int, Category> categoriesById,
        string devise)
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

        string normalizedDevise = string.IsNullOrWhiteSpace(devise)
            ? "EUR"
            : devise.Trim().ToUpperInvariant();

        return new ExpenseListItemViewModel
        {
            Id = expense.Id,
            CategoryId = expense.CategoryId,
            Amount = expense.Amount,
            FormattedAmount = CurrencyHelper.Format(expense.Amount, normalizedDevise),
            ExpenseDate = expense.DateOperation,
            FormattedDate = expense.DateOperation.ToString("dd/MM/yyyy"),
            Note = note,
            DisplayName = displayName,
            CategoryName = categoryName,
            CategoryIcon = string.IsNullOrWhiteSpace(category?.Icon) ? "💰" : category!.Icon,
            CategoryColor = TryCreateColor(category?.Color),
            IconBackgroundColor = "#EEF2F5",
            AmountColor = "#D9534F",
            Type = "Dépense",
            IsExpense = true,
            IsFixedCharge = expense.IsFixedCharge,
            Devise = normalizedDevise
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
