using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace UnitTests.Services
{
    [TestClass]
    public class DashboardServiceTests
    {
        [TestMethod]
        public async Task GetDashboardSummaryAsync_WithInvalidUser_ReturnsFailure()
        {
            DashboardService service = new(new Mock<IMoneyMateDbContext>().Object);

            var result = await service.GetDashboardSummaryAsync(0);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("DASHBOARD_INVALID_USER", result.ErrorCode);
        }

        [TestMethod]
        public async Task GetDashboardSummaryAsync_WithValidData_ReturnsExpectedSummary()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            DateTime now = DateTime.Now;
            DateTime currentMonthExpenseDate = new(now.Year, now.Month, 10);
            DateTime previousMonthExpenseDate = currentMonthExpenseDate.AddMonths(-1);

            dbContextMock.Setup(x => x.GetExpensesByUserId(1))
                .Returns(new List<Expense>
                {
                    new()
                    {
                        Id = 1,
                        UserId = 1,
                        CategoryId = 2,
                        Amount = 100m,
                        DateOperation = currentMonthExpenseDate
                    },
                    new()
                    {
                        Id = 2,
                        UserId = 1,
                        CategoryId = 2,
                        Amount = 50m,
                        DateOperation = currentMonthExpenseDate.AddDays(1)
                    },
                    new()
                    {
                        Id = 3,
                        UserId = 1,
                        CategoryId = 3,
                        Amount = 70m,
                        DateOperation = previousMonthExpenseDate
                    }
                });

            dbContextMock.Setup(x => x.GetBudgetsByUserId(1))
                .Returns(new List<Budget>
                {
                    new()
                    {
                        Id = 10,
                        UserId = 1,
                        CategoryId = 2,
                        Amount = 180m,
                        PeriodType = "Monthly",
                        StartDate = new DateTime(now.Year, now.Month, 1),
                        EndDate = new DateTime(now.Year, now.Month, 28),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                });

            dbContextMock.Setup(x => x.GetAlertThresholdsByUserId(1))
                .Returns(new List<AlertThreshold>
                {
                    new()
                    {
                        Id = 20,
                        UserId = 1,
                        BudgetId = 10,
                        CategoryId = 2,
                        ThresholdPercentage = 80m,
                        AlertType = "Warning",
                        IsActive = true
                    }
                });

            dbContextMock.Setup(x => x.GetActiveFixedChargesCountByUserId(1))
                .Returns(1);

            dbContextMock.Setup(x => x.GetCategoriesByUserId(1))
                .Returns(new List<Category>
                {
                    new()
                    {
                        Id = 2,
                        UserId = 1,
                        Name = "Courses",
                        IsActive = true
                    },
                    new()
                    {
                        Id = 3,
                        UserId = 1,
                        Name = "Transport",
                        IsActive = true
                    }
                });

            DashboardService service = new(dbContextMock.Object);

            var result = await service.GetDashboardSummaryAsync(1);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(150m, result.Data.CurrentMonthExpenses);
            Assert.AreEqual(2, result.Data.CurrentMonthExpensesCount);
            Assert.AreEqual(70m, result.Data.PreviousMonthExpenses);
            Assert.AreEqual(80m, result.Data.ExpensesDeltaFromPreviousMonth);
            Assert.AreEqual(1, result.Data.ActiveBudgetsCount);
            Assert.AreEqual(1, result.Data.ActiveFixedChargesCount);
            Assert.AreEqual(1, result.Data.ActiveAlertsCount);
            Assert.AreEqual(1, result.Data.TriggeredAlertsCount);
            Assert.AreEqual(1, result.Data.BudgetsAtRiskCount);
            Assert.AreEqual(1, result.Data.TopCategories.Count);
            Assert.AreEqual("Courses", result.Data.TopCategories[0].CategoryName);
            Assert.AreEqual(150m, result.Data.TopCategories[0].TotalAmount);
        }

        [TestMethod]
        public async Task GetTopSpendingCategoriesAsync_WithTopCountZero_ReturnsEmptyList()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            DashboardService service = new(dbContextMock.Object);

            var result = await service.GetTopSpendingCategoriesAsync(1, 0);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(0, result.Data.Count);
        }
    }
}
