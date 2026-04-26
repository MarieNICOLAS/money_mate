using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Common;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour l'alimentation du tableau de bord.
    /// Version optimisée pour limiter les relectures SQLite.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private const decimal BudgetAtRiskThreshold = 80m;

        private readonly IMoneyMateDbContext _dbContext;

        public DashboardService()
            : this(DbContextFactory.CreateDefault())
        {
        }

        public DashboardService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public Task<ServiceResult<DashboardSummary>> GetDashboardSummaryAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                    {
                        return ServiceResult<DashboardSummary>.Failure(
                            "DASHBOARD_INVALID_USER",
                            ServiceMessages.InvalidUser);
                    }

                    DateTime now = DateTime.Now;
                    DateTime monthStart = new(now.Year, now.Month, 1);
                    DateTime nextMonthStart = monthStart.AddMonths(1);
                    DateTime previousMonthStart = monthStart.AddMonths(-1);

                    List<Expense> allExpenses = _dbContext.GetExpensesByUserId(userId);
                    List<Budget> allBudgets = _dbContext.GetBudgetsByUserId(userId);
                    List<Budget> budgets = allBudgets
                        .Where(budget => budget.IsActive)
                        .ToList();
                    List<AlertThreshold> alertThresholds = _dbContext.GetAlertThresholdsByUserId(userId);
                    int activeFixedChargesCount = _dbContext.GetActiveFixedChargesCountByUserId(userId);
                    Dictionary<int, Category> categoriesById = _dbContext.GetCategoriesByUserId(userId)
                        .ToDictionary(category => category.Id, category => category);

                    foreach (Budget budget in allBudgets)
                        budget.NormalizeToMonthlyPeriod();

                    List<Expense> currentMonthExpenses = allExpenses
                        .Where(expense => expense.DateOperation >= monthStart && expense.DateOperation < nextMonthStart)
                        .ToList();

                    List<Expense> previousMonthExpenses = allExpenses
                        .Where(expense => expense.DateOperation >= previousMonthStart && expense.DateOperation < monthStart)
                        .ToList();

                    decimal currentMonthExpensesAmount = currentMonthExpenses.Sum(expense => expense.Amount);
                    decimal previousMonthExpensesAmount = previousMonthExpenses.Sum(expense => expense.Amount);

                    List<DashboardCategorySpending> topCategories = BuildTopCategories(
                        currentMonthExpenses,
                        categoriesById,
                        topCount: 5);

                    bool hasCurrentMonthBudget = allBudgets.Any(budget =>
                        budget.StartDate < nextMonthStart &&
                        (budget.EndDate ?? DateTime.MaxValue) >= monthStart);

                    List<Budget> currentMonthBudgets = budgets
                        .Where(budget => budget.StartDate < nextMonthStart && (budget.EndDate ?? DateTime.MaxValue) >= monthStart)
                        .ToList();

                    decimal currentMonthBudget = currentMonthBudgets.Sum(budget => budget.Amount);

                    List<DashboardRecentTransaction> recentTransactions = BuildRecentTransactions(
                        currentMonthExpenses,
                        categoriesById,
                        take: 5);

                    int budgetsAtRiskCount = budgets.Count(budget =>
                        IsBudgetAtRisk(budget, allExpenses));

                    int triggeredAlertsCount = alertThresholds.Count(alertThreshold =>
                        IsAlertTriggered(alertThreshold, budgets, allExpenses, now));

                    DashboardSummary summary = new()
                    {
                        CurrentMonthExpenses = currentMonthExpensesAmount,
                        CurrentMonthBudget = currentMonthBudget,
                        HasCurrentMonthBudget = hasCurrentMonthBudget,
                        CurrentMonthBalance = currentMonthBudget > 0
                            ? currentMonthBudget - currentMonthExpensesAmount
                            : -currentMonthExpensesAmount,
                        CurrentMonthExpensesCount = currentMonthExpenses.Count,
                        PreviousMonthExpenses = previousMonthExpensesAmount,
                        ExpensesDeltaFromPreviousMonth = currentMonthExpensesAmount - previousMonthExpensesAmount,
                        ActiveBudgetsCount = budgets.Count,
                        ActiveFixedChargesCount = activeFixedChargesCount,
                        ActiveAlertsCount = alertThresholds.Count,
                        TriggeredAlertsCount = triggeredAlertsCount,
                        BudgetsAtRiskCount = budgetsAtRiskCount,
                        TopCategories = topCategories,
                        RecentTransactions = recentTransactions
                    };

                    return ServiceResult<DashboardSummary>.Success(summary);
                },
                operationName: nameof(GetDashboardSummaryAsync),
                fallbackErrorCode: "DASHBOARD_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement du tableau de bord.");
        }

        public Task<ServiceResult<List<DashboardCategorySpending>>> GetTopSpendingCategoriesAsync(int userId, int topCount = 5)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                    {
                        return ServiceResult<List<DashboardCategorySpending>>.Failure(
                            "DASHBOARD_INVALID_USER",
                            ServiceMessages.InvalidUser);
                    }

                    if (topCount <= 0)
                        return ServiceResult<List<DashboardCategorySpending>>.Success([]);

                    DateTime now = DateTime.Now;
                    DateTime monthStart = new(now.Year, now.Month, 1);
                    DateTime nextMonthStart = monthStart.AddMonths(1);

                    List<Expense> currentMonthExpenses = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.DateOperation >= monthStart && expense.DateOperation < nextMonthStart)
                        .ToList();

                    Dictionary<int, Category> categoriesById = _dbContext.GetCategoriesByUserId(userId)
                        .ToDictionary(category => category.Id, category => category);

                    List<DashboardCategorySpending> categories = BuildTopCategories(
                        currentMonthExpenses,
                        categoriesById,
                        topCount);

                    return ServiceResult<List<DashboardCategorySpending>>.Success(categories);
                },
                operationName: nameof(GetTopSpendingCategoriesAsync),
                fallbackErrorCode: "DASHBOARD_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des catégories du tableau de bord.");
        }

        public Task<ServiceResult<List<DashboardRecentTransaction>>> GetRecentTransactionsAsync(int userId, int take = 5)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                    {
                        return ServiceResult<List<DashboardRecentTransaction>>.Failure(
                            "DASHBOARD_INVALID_USER",
                            ServiceMessages.InvalidUser);
                    }

                    if (take <= 0)
                        return ServiceResult<List<DashboardRecentTransaction>>.Success([]);

                    Dictionary<int, Category> categoriesById = _dbContext.GetCategoriesByUserId(userId)
                        .ToDictionary(category => category.Id, category => category);

                    List<DashboardRecentTransaction> transactions = BuildRecentTransactions(
                        _dbContext.GetExpensesByUserId(userId),
                        categoriesById,
                        take);

                    return ServiceResult<List<DashboardRecentTransaction>>.Success(transactions);
                },
                operationName: nameof(GetRecentTransactionsAsync),
                fallbackErrorCode: "DASHBOARD_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des transactions récentes.");
        }

        private static List<DashboardCategorySpending> BuildTopCategories(
            IEnumerable<Expense> expenses,
            IReadOnlyDictionary<int, Category> categoriesById,
            int topCount)
        {
            return expenses
                .GroupBy(expense => expense.CategoryId)
                .Select(group => new DashboardCategorySpending
                {
                    CategoryId = group.Key,
                    CategoryName = categoriesById.TryGetValue(group.Key, out Category? category)
                        ? category.Name
                        : "Catégorie inconnue",
                    CategoryColor = categoriesById.TryGetValue(group.Key, out category)
                        ? category.Color
                        : "#6B7A8F",
                    CategoryIcon = categoriesById.TryGetValue(group.Key, out category)
                        ? category.Icon
                        : "💰",
                    TotalAmount = group.Sum(expense => expense.Amount),
                    ExpensesCount = group.Count()
                })
                .OrderByDescending(item => item.TotalAmount)
                .ThenByDescending(item => item.ExpensesCount)
                .Take(topCount)
                .ToList();
        }

        private static List<DashboardRecentTransaction> BuildRecentTransactions(
            IEnumerable<Expense> expenses,
            IReadOnlyDictionary<int, Category> categoriesById,
            int take)
        {
            return expenses
                .OrderByDescending(expense => expense.DateOperation)
                .Take(take)
                .Select(expense =>
                {
                    categoriesById.TryGetValue(expense.CategoryId, out Category? category);

                    return new DashboardRecentTransaction
                    {
                        ExpenseId = expense.Id,
                        CategoryName = category?.Name ?? "Catégorie inconnue",
                        CategoryColor = category?.Color ?? "#6B7A8F",
                        CategoryIcon = category?.Icon ?? "💰",
                        Note = expense.Note,
                        Amount = expense.Amount,
                        DateOperation = expense.DateOperation
                    };
                })
                .ToList();
        }

        private static bool IsBudgetAtRisk(Budget budget, IEnumerable<Expense> allExpenses)
        {
            if (budget.Amount <= 0)
                return false;

            DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

            decimal consumedAmount = allExpenses
                .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate)
                .Sum(expense => expense.Amount);

            return budget.CalculateBudgetPercentage(consumedAmount) >= BudgetAtRiskThreshold;
        }

        private static bool IsAlertTriggered(
            AlertThreshold alertThreshold,
            IReadOnlyList<Budget> budgets,
            IEnumerable<Expense> allExpenses,
            DateTime referenceDate)
        {
            Budget? budget = ResolveBudget(alertThreshold, budgets, referenceDate);

            if (budget is null || budget.Amount <= 0)
                return false;

            DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

            IEnumerable<Expense> scopedExpenses = allExpenses
                .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate);

            if (alertThreshold.CategoryId.HasValue)
            {
                int categoryId = alertThreshold.CategoryId.Value;
                scopedExpenses = scopedExpenses.Where(expense => expense.CategoryId == categoryId);
            }

            decimal consumedAmount = scopedExpenses.Sum(expense => expense.Amount);

            return budget.CalculateBudgetPercentage(consumedAmount) >= alertThreshold.ThresholdPercentage;
        }

        private static Budget? ResolveBudget(
            AlertThreshold alertThreshold,
            IReadOnlyList<Budget> budgets,
            DateTime referenceDate)
        {
            if (alertThreshold.BudgetId.HasValue)
            {
                int budgetId = alertThreshold.BudgetId.Value;
                return budgets.FirstOrDefault(budget => budget.Id == budgetId);
            }

            return budgets
                .Where(budget => budget.StartDate.Year == referenceDate.Year && budget.StartDate.Month == referenceDate.Month)
                .OrderByDescending(budget => budget.CreatedAt)
                .FirstOrDefault();
        }
    }
}
