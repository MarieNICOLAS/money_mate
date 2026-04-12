using MoneyMate.Data.Context;
using MoneyMate.Models;
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
        private readonly IMoneyMateDbContext _dbContext;

        public DashboardService()
            : this(DatabaseService.Instance)
        {
        }

        public DashboardService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
                    DateTime previousMonthStart = monthStart.AddMonths(-1);

                    List<Expense> userExpenses = _dbContext.GetExpensesByUserId(userId);

                    List<Expense> monthlyExpenses = userExpenses
                        .Where(expense => expense.DateOperation >= monthStart && expense.DateOperation < nextMonthStart)
                        .ToList();

                    decimal currentMonthExpenses = monthlyExpenses.Sum(expense => expense.Amount);

                    decimal previousMonthExpenses = userExpenses
                        .Where(expense => expense.DateOperation >= previousMonthStart && expense.DateOperation < monthStart)
                        .Sum(expense => expense.Amount);

                    List<Budget> budgets = _dbContext.GetBudgetsByUserId(userId);
                    List<AlertThreshold> alertThresholds = _dbContext.GetAlertThresholdsByUserId(userId);
                    int budgetsAtRiskCount = budgets.Count(budget => IsBudgetAtRisk(userId, budget));
                    int triggeredAlertsCount = alertThresholds.Count(alertThreshold => IsAlertTriggered(userId, alertThreshold));
                    ServiceResult<List<DashboardCategorySpending>> topCategoriesResult = GetTopSpendingCategoriesInternal(userId, 5, monthStart, nextMonthStart);

                    DashboardSummary summary = new()
                    {
                        CurrentMonthExpenses = currentMonthExpenses,
                        CurrentMonthExpensesCount = monthlyExpenses.Count,
                        PreviousMonthExpenses = previousMonthExpenses,
                        ExpensesDeltaFromPreviousMonth = currentMonthExpenses - previousMonthExpenses,
                        ActiveBudgetsCount = budgets.Count,
                        ActiveFixedChargesCount = _dbContext.GetFixedChargesByUserId(userId).Count,
                        ActiveAlertsCount = alertThresholds.Count,
                        TriggeredAlertsCount = triggeredAlertsCount,
                        BudgetsAtRiskCount = budgetsAtRiskCount,
                        TopCategories = topCategoriesResult.IsSuccess && topCategoriesResult.Data != null
                            ? topCategoriesResult.Data
                            : []
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

        public async Task<ServiceResult<List<DashboardCategorySpending>>> GetTopSpendingCategoriesAsync(int userId, int topCount = 5)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<DashboardCategorySpending>>.Failure("DASHBOARD_INVALID_USER", "Utilisateur invalide.");

                    DateTime now = DateTime.Now;
                    DateTime monthStart = new(now.Year, now.Month, 1);
                    DateTime nextMonthStart = monthStart.AddMonths(1);

                    return GetTopSpendingCategoriesInternal(userId, topCount, monthStart, nextMonthStart);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetTopSpendingCategoriesAsync : {ex.Message}");
                    return ServiceResult<List<DashboardCategorySpending>>.Failure("DASHBOARD_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des catégories du tableau de bord.");
                }
            });
        }

        private ServiceResult<List<DashboardCategorySpending>> GetTopSpendingCategoriesInternal(int userId, int topCount, DateTime startDate, DateTime endDate)
        {
            if (topCount <= 0)
                return ServiceResult<List<DashboardCategorySpending>>.Success([]);

            Dictionary<int, string> categoriesById = _dbContext.GetCategoriesByUserId(userId)
                .ToDictionary(category => category.Id, category => category.Name);

            List<DashboardCategorySpending> categories = _dbContext.GetExpensesByUserId(userId)
                .Where(expense => expense.DateOperation >= startDate && expense.DateOperation < endDate)
                .GroupBy(expense => expense.CategoryId)
                .Select(group => new DashboardCategorySpending
                {
                    CategoryId = group.Key,
                    CategoryName = categoriesById.TryGetValue(group.Key, out string? categoryName) ? categoryName : "Catégorie inconnue",
                    TotalAmount = group.Sum(expense => expense.Amount),
                    ExpensesCount = group.Count()
                })
                .OrderByDescending(item => item.TotalAmount)
                .ThenByDescending(item => item.ExpensesCount)
                .Take(topCount)
                .ToList();

            return ServiceResult<List<DashboardCategorySpending>>.Success(categories);
        }

        private bool IsBudgetAtRisk(int userId, Budget budget)
        {
            if (budget.Amount <= 0)
                return false;

            budget.NormalizeToMonthlyPeriod();

            DateTime endDate = budget.EndDate ?? DateTime.MaxValue;
            decimal consumedAmount = _dbContext.GetExpensesByUserId(userId)
                .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate)
                .Sum(expense => expense.Amount);

            return budget.CalculateBudgetPercentage(consumedAmount) >= 80m;
        }

        private bool IsAlertTriggered(int userId, AlertThreshold alertThreshold)
        {
            Budget? budget = ResolveBudget(userId, alertThreshold);
            if (budget == null || budget.Amount <= 0)
                return false;

            budget.NormalizeToMonthlyPeriod();
            DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

            IEnumerable<Expense> expenses = _dbContext.GetExpensesByUserId(userId)
                .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate);

            if (alertThreshold.CategoryId.HasValue)
                expenses = expenses.Where(expense => expense.CategoryId == alertThreshold.CategoryId.Value);

            decimal consumedAmount = expenses.Sum(expense => expense.Amount);

            return budget.CalculateBudgetPercentage(consumedAmount) >= alertThreshold.ThresholdPercentage;
        }

        private Budget? ResolveBudget(int userId, AlertThreshold alertThreshold)
        {
            if (alertThreshold.BudgetId.HasValue)
                return _dbContext.GetBudgetById(alertThreshold.BudgetId.Value, userId);

            DateTime now = DateTime.Now;

            return _dbContext.GetBudgetsByUserId(userId)
                .Where(budget => budget.StartDate.Year == now.Year && budget.StartDate.Month == now.Month)
                .OrderByDescending(budget => budget.CreatedAt)
                .FirstOrDefault();
        }
    }
}
