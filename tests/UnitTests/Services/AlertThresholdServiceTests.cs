using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace UnitTests.Services
{
    [TestClass]
    public class AlertThresholdServiceTests
    {
        [TestMethod]
        public async Task CreateAlertThresholdAsync_WithoutTarget_ReturnsFailure()
        {
            AlertThresholdService service = new(new Mock<IMoneyMateDbContext>().Object);

            AlertThreshold alertThreshold = new()
            {
                UserId = 1,
                ThresholdPercentage = 80,
                AlertType = "Warning"
            };

            var result = await service.CreateAlertThresholdAsync(alertThreshold);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("ALERT_TARGET_REQUIRED", result.ErrorCode);
        }

        [TestMethod]
        public async Task CreateAlertThresholdAsync_WithBudgetAndCategoryTargets_ReturnsSuccess()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetBudgetById(4, 1))
                .Returns(new Budget
                {
                    Id = 4,
                    UserId = 1,
                    Amount = 300m,
                    PeriodType = "Monthly",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(1)
                });

            dbContextMock.Setup(x => x.GetCategoryById(3, 1))
                .Returns(new Category
                {
                    Id = 3,
                    UserId = 1,
                    Name = "Transport",
                    IsActive = true
                });

            dbContextMock.Setup(x => x.InsertAlertThreshold(It.IsAny<AlertThreshold>()))
                .Callback<AlertThreshold>(alert => alert.Id = 12)
                .Returns(12);

            AlertThresholdService service = new(dbContextMock.Object);

            AlertThreshold alertThreshold = new()
            {
                UserId = 1,
                BudgetId = 4,
                CategoryId = 3,
                ThresholdPercentage = 90,
                AlertType = "Warning"
            };

            var result = await service.CreateAlertThresholdAsync(alertThreshold);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(12, result.Data.Id);
        }

        [TestMethod]
        public async Task CreateAlertThresholdAsync_WithThresholdZero_ReturnsSuccess()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetBudgetById(5, 1))
                .Returns(new Budget
                {
                    Id = 5,
                    UserId = 1,
                    Amount = 500m,
                    PeriodType = "Monthly",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(1)
                });

            dbContextMock.Setup(x => x.GetCategoryById(2, 1))
                .Returns(new Category
                {
                    Id = 2,
                    UserId = 1,
                    Name = "Courses",
                    IsActive = true
                });

            dbContextMock.Setup(x => x.InsertAlertThreshold(It.IsAny<AlertThreshold>()))
                .Callback<AlertThreshold>(alert => alert.Id = 11)
                .Returns(11);

            AlertThresholdService service = new(dbContextMock.Object);

            AlertThreshold alertThreshold = new()
            {
                UserId = 1,
                BudgetId = 5,
                CategoryId = 2,
                ThresholdPercentage = 0,
                AlertType = "Warning"
            };

            var result = await service.CreateAlertThresholdAsync(alertThreshold);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(11, result.Data.Id);
        }
    }
}
