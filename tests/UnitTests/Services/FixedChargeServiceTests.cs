using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace UnitTests.Services
{
    [TestClass]
    public class FixedChargeServiceTests
    {
        [TestMethod]
        public async Task CreateFixedChargeAsync_WithInvalidDay_ReturnsFailure()
        {
            FixedChargeService service = new(new Mock<IMoneyMateDbContext>().Object);

            FixedCharge fixedCharge = new()
            {
                UserId = 1,
                Name = "Netflix",
                CategoryId = 2,
                Amount = 15.99m,
                Frequency = "Monthly",
                DayOfMonth = 0,
                StartDate = DateTime.Today
            };

            var result = await service.CreateFixedChargeAsync(fixedCharge);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("FIXED_CHARGE_INVALID_DAY", result.ErrorCode);
        }

        [TestMethod]
        public async Task CreateFixedChargeAsync_WithInactiveCategory_ReturnsCategoryNotFound()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetCategoryById(2, 1))
                .Returns(new Category
                {
                    Id = 2,
                    UserId = 1,
                    Name = "Abonnements",
                    IsActive = false
                });

            FixedChargeService service = new(dbContextMock.Object);

            FixedCharge fixedCharge = new()
            {
                UserId = 1,
                Name = "Spotify",
                CategoryId = 2,
                Amount = 10.99m,
                Frequency = "Monthly",
                DayOfMonth = 5,
                StartDate = DateTime.Today
            };

            var result = await service.CreateFixedChargeAsync(fixedCharge);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("FIXED_CHARGE_CATEGORY_NOT_FOUND", result.ErrorCode);
        }
    }
}
