namespace MoneyMate.ViewModels;

/// <summary>
/// Clés de paramètres de navigation partagées entre les ViewModels.
/// </summary>
public static class NavigationParameterKeys
{
    public const string CategoryId = nameof(CategoryId);
    public const string ExpenseId = nameof(ExpenseId);
    public const string BudgetId = nameof(BudgetId);
    public const string FixedChargeId = nameof(FixedChargeId);
    public const string AlertThresholdId = nameof(AlertThresholdId);
    public const string ReturnRoute = nameof(ReturnRoute);
}
