using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace IntegrationTests.Services
{
    [TestClass]
    public class AlertThresholdServiceIntegrationTests : TestDatabaseFixture
    {
        [TestMethod]
        public async Task CreateAlertThresholdAsync_WithThresholdZero_Succeeds()
        {
            int userId = CreateUser();
            Category category = CreateCategory(userId);
            Budget budget = CreateBudget(
                userId,
                category.Id,
                DateTime.Today,
                DateTime.Today.AddMonths(1),
                500m);

            AlertThresholdService service = new(DbContext);

            AlertThreshold alertThreshold = new()
            {
                UserId = userId,
                BudgetId = budget.Id,
                CategoryId = category.Id,
                ThresholdPercentage = 0,
                AlertType = "Warning"
            };

            var result = await service.CreateAlertThresholdAsync(alertThreshold);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);

            AlertThreshold? persistedAlert = DbContext.GetAlertThresholdById(result.Data.Id, userId);

            Assert.IsNotNull(persistedAlert);
            Assert.AreEqual(0m, persistedAlert.ThresholdPercentage);
        }

        [TestMethod]
        public async Task CreateAlertThresholdAsync_WithoutTarget_ReturnsFailure()
        {
            int userId = CreateUser();

            AlertThresholdService service = new(DbContext);

            AlertThreshold alertThreshold = new()
            {
                UserId = userId,
                ThresholdPercentage = 80,
                AlertType = "Warning"
            };

            var result = await service.CreateAlertThresholdAsync(alertThreshold);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("ALERT_TARGET_REQUIRED", result.ErrorCode);
        }
    }
}
