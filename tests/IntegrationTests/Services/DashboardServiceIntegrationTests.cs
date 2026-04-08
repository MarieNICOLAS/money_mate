using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace IntegrationTests.Services
{
    [TestClass]
    public class DashboardServiceIntegrationTests : TestDatabaseFixture
    {
        [TestMethod]
        public async Task GetDashboardSummaryAsync_WithPersistedData_ReturnsExpectedSummary()
        {
            int userId = CreateUser();
            Category foodCategory = CreateCategory(userId, name: "Courses");
            Category transportCategory = CreateCategory(userId, name: "Transport");

            DateTime now = DateTime.Now;
            DateTime currentMonthDate = new(now.Year, now.Month, 10);
            DateTime previousMonthDate = currentMonthDate.AddMonths(-1);

            CreateBudget(
                userId,
                foodCategory.Id,
                new DateTime(now.Year, now.Month, 1),
                new DateTime(now.Year, now.Month, 28),
                180m);

            DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = foodCategory.Id,
                Amount = 100m,
                Note = "Courses 1",
                DateOperation = currentMonthDate
            });

            DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = foodCategory.Id,
                Amount = 50m,
                Note = "Courses 2",
                DateOperation = currentMonthDate.AddDays(1)
            });

            DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = transportCategory.Id,
                Amount = 70m,
                Note = "Transport mois précédent",
                DateOperation = previousMonthDate
            });

            DbContext.InsertFixedCharge(new FixedCharge
            {
                UserId = userId,
                Name = "Netflix",
                Description = "Abonnement",
                CategoryId = foodCategory.Id,
                Amount = 15.99m,
                Frequency = "Monthly",
                DayOfMonth = 5,
                StartDate = currentMonthDate,
                IsActive = true,
                AutoCreateExpense = true,
                CreatedAt = DateTime.UtcNow
            });

            Budget? persistedBudget = DbContext.GetBudgetsByUserId(userId)
                .FirstOrDefault();

            Assert.IsNotNull(persistedBudget);

            DbContext.InsertAlertThreshold(new AlertThreshold
            {
                UserId = userId,
                BudgetId = persistedBudget.Id,
                CategoryId = foodCategory.Id,
                ThresholdPercentage = 80m,
                AlertType = "Warning",
                Message = "Alerte test",
                IsActive = true,
                SendNotification = true,
                CreatedAt = DateTime.UtcNow
            });

            DashboardService service = new(DbContext);

            var result = await service.GetDashboardSummaryAsync(userId);

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
        public async Task GetTopSpendingCategoriesAsync_WithPersistedData_ReturnsOrderedCategories()
        {
            int userId = CreateUser();
            Category foodCategory = CreateCategory(userId, name: "Courses");
            Category transportCategory = CreateCategory(userId, name: "Transport");

            DateTime now = DateTime.Now;
            DateTime currentMonthDate = new(now.Year, now.Month, 12);

            DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = transportCategory.Id,
                Amount = 40m,
                Note = "Bus",
                DateOperation = currentMonthDate
            });

            DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = foodCategory.Id,
                Amount = 120m,
                Note = "Courses",
                DateOperation = currentMonthDate
            });

            DashboardService service = new(DbContext);

            var result = await service.GetTopSpendingCategoriesAsync(userId, 2);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(2, result.Data.Count);
            Assert.AreEqual("Courses", result.Data[0].CategoryName);
            Assert.AreEqual(120m, result.Data[0].TotalAmount);
            Assert.AreEqual("Transport", result.Data[1].CategoryName);
            Assert.AreEqual(40m, result.Data[1].TotalAmount);
        }
    }
}
