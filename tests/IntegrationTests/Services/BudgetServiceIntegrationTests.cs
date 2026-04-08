using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace IntegrationTests.Services
{
    [TestClass]
    public class BudgetServiceIntegrationTests : TestDatabaseFixture
    {
        [TestMethod]
        public async Task CreateBudgetAsync_WithOverlappingBudget_ReturnsConflict()
        {
            int userId = CreateUser();
            Category category = CreateCategory(userId);

            CreateBudget(
                userId,
                category.Id,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 1, 31),
                250m);

            BudgetService service = new(DbContext);

            Budget budget = new()
            {
                UserId = userId,
                CategoryId = category.Id,
                Amount = 300m,
                PeriodType = "Monthly",
                StartDate = new DateTime(2026, 1, 15),
                EndDate = new DateTime(2026, 2, 15)
            };

            var result = await service.CreateBudgetAsync(budget);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("BUDGET_CONFLICT", result.ErrorCode);
        }

        [TestMethod]
        public async Task CreateBudgetAsync_WithValidBudget_PersistsBudget()
        {
            int userId = CreateUser();
            Category category = CreateCategory(userId);

            BudgetService service = new(DbContext);

            Budget budget = new()
            {
                UserId = userId,
                CategoryId = category.Id,
                Amount = 450m,
                PeriodType = "Monthly",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1)
            };

            var result = await service.CreateBudgetAsync(budget);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);

            Budget? persistedBudget = DbContext.GetBudgetById(result.Data.Id, userId);

            Assert.IsNotNull(persistedBudget);
            Assert.AreEqual(450m, persistedBudget.Amount);
        }
    }
}
