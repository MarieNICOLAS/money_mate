using MoneyMate.Data.Context;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour l'alimentation du tableau de bord.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly MoneyMateDbContext _dbContext;

        public DashboardService()
        {
            _dbContext = DatabaseService.Instance;
        }

        public async Task<ServiceResult<DashboardSummary>> GetDashboardSummaryAsync(int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<DashboardSummary>.Failure("DASHBOARD_INVALID_USER", "Utilisateur invalide.");

                    DateTime now = DateTime.Now;
                    DateTime monthStart = new(now.Year, now.Month, 1);
                    DateTime nextMonthStart = monthStart.AddMonths(1);

                    List<global::MoneyMate.Models.Expense> monthlyExpenses = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.DateOperation >= monthStart && expense.DateOperation < nextMonthStart)
                        .ToList();

                    DashboardSummary summary = new()
                    {
                        CurrentMonthExpenses = monthlyExpenses.Sum(expense => expense.Amount),
                        CurrentMonthExpensesCount = monthlyExpenses.Count,
                        ActiveBudgetsCount = _dbContext.GetBudgetsByUserId(userId).Count,
                        ActiveFixedChargesCount = _dbContext.GetFixedChargesByUserId(userId).Count,
                        ActiveAlertsCount = _dbContext.GetAlertThresholdsByUserId(userId).Count
                    };

                    return ServiceResult<DashboardSummary>.Success(summary);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetDashboardSummaryAsync : {ex.Message}");
                    return ServiceResult<DashboardSummary>.Failure("DASHBOARD_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement du tableau de bord.");
                }
            });
        }
    }
}
