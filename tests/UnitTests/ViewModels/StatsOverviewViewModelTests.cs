using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Models;
using MoneyMate.ViewModels.Stats;

namespace UnitTests.ViewModels;

[TestClass]
public class StatsOverviewViewModelTests
{
    [TestMethod]
    public void BuildMonthlyStats_UsesCurrentMonthExpensesAndCalculatesNetBalance()
    {
        DateTime monthStart = new(2026, 4, 1);
        DateTime nextMonthStart = monthStart.AddMonths(1);

        List<Expense> expenses =
        [
            new() { Amount = 120m, CategoryId = 1, DateOperation = new DateTime(2026, 4, 4) },
            new() { Amount = 80m, CategoryId = 2, DateOperation = new DateTime(2026, 4, 18) },
            new() { Amount = 500m, CategoryId = 1, DateOperation = new DateTime(2026, 3, 30) }
        ];

        List<Budget> budgets =
        [
            new() { Amount = 900m, StartDate = monthStart, EndDate = nextMonthStart.AddDays(-1), IsActive = true },
            new() { Amount = 300m, StartDate = monthStart, EndDate = nextMonthStart.AddDays(-1), IsActive = false }
        ];

        MonthlyStatsDto stats = StatsOverviewViewModel.BuildMonthlyStats(
            expenses,
            budgets,
            monthStart,
            nextMonthStart);

        Assert.AreEqual(900m, stats.IncomeAmount);
        Assert.AreEqual(200m, stats.ExpenseAmount);
        Assert.AreEqual(700m, stats.NetBalance);
        Assert.AreEqual(2, stats.ExpensesCount);
        Assert.IsTrue(stats.HasIncomeSource);
    }

    [TestMethod]
    public void BuildCategoryStats_KeepsMaximumFiveItemsAndGroupsOthers()
    {
        List<Expense> expenses =
        [
            new() { Amount = 50m, CategoryId = 1, DateOperation = new DateTime(2026, 4, 1) },
            new() { Amount = 40m, CategoryId = 2, DateOperation = new DateTime(2026, 4, 2) },
            new() { Amount = 30m, CategoryId = 3, DateOperation = new DateTime(2026, 4, 3) },
            new() { Amount = 20m, CategoryId = 4, DateOperation = new DateTime(2026, 4, 4) },
            new() { Amount = 10m, CategoryId = 5, DateOperation = new DateTime(2026, 4, 5) },
            new() { Amount = 10m, CategoryId = 6, DateOperation = new DateTime(2026, 4, 6) }
        ];

        List<Category> categories =
        [
            new() { Id = 1, Name = "Courses", Color = "#5CB85C" },
            new() { Id = 2, Name = "Transport", Color = "#6B7A8F" },
            new() { Id = 3, Name = "Loisirs", Color = "#D9534F" },
            new() { Id = 4, Name = "Maison", Color = "#8A9BAF" },
            new() { Id = 5, Name = "Sante", Color = "#6B7A8F" },
            new() { Id = 6, Name = "Divers", Color = "#6B7A8F" }
        ];

        List<CategoryStatsDto> stats = StatsOverviewViewModel.BuildCategoryStats(expenses, categories);

        Assert.AreEqual(5, stats.Count);
        Assert.AreEqual("Courses", stats[0].CategoryName);
        Assert.AreEqual("Autres", stats[4].CategoryName);
        Assert.AreEqual(20m, stats[4].Amount);
        Assert.AreEqual(12.5m, stats[4].Percentage);
    }

    [TestMethod]
    public void BuildCategoryStats_WithEmptyExpenses_ReturnsEmptyAndAvoidsDivisionByZero()
    {
        List<CategoryStatsDto> stats = StatsOverviewViewModel.BuildCategoryStats([], []);

        Assert.AreEqual(0, stats.Count);
        Assert.AreEqual(0m, StatsOverviewViewModel.CalculatePercentage(10m, 0m));
    }

    [TestMethod]
    public void BuildMonthlyStats_WithEmptyData_ReturnsZeroValues()
    {
        DateTime monthStart = new(2026, 4, 1);

        MonthlyStatsDto stats = StatsOverviewViewModel.BuildMonthlyStats(
            [],
            [],
            monthStart,
            monthStart.AddMonths(1));

        Assert.AreEqual(0m, stats.IncomeAmount);
        Assert.AreEqual(0m, stats.ExpenseAmount);
        Assert.AreEqual(0m, stats.NetBalance);
        Assert.AreEqual(0, stats.ExpensesCount);
        Assert.IsFalse(stats.HasIncomeSource);
    }
}
