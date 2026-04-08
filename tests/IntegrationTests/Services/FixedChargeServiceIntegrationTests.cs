using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace IntegrationTests.Services
{
    [TestClass]
    public class FixedChargeServiceIntegrationTests : TestDatabaseFixture
    {
        [TestMethod]
        public async Task CreateFixedChargeAsync_WithInactiveCategory_ReturnsCategoryNotFound()
        {
            int userId = CreateUser();
            Category category = CreateCategory(userId, isActive: false);

            FixedChargeService service = new(DbContext);

            FixedCharge fixedCharge = new()
            {
                UserId = userId,
                Name = "Netflix",
                CategoryId = category.Id,
                Amount = 15.99m,
                Frequency = "Monthly",
                DayOfMonth = 5,
                StartDate = DateTime.Today
            };

            var result = await service.CreateFixedChargeAsync(fixedCharge);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("FIXED_CHARGE_CATEGORY_NOT_FOUND", result.ErrorCode);
        }

        [TestMethod]
        public async Task CreateFixedChargeAsync_WithValidFixedCharge_PersistsEntity()
        {
            int userId = CreateUser();
            Category category = CreateCategory(userId);

            FixedChargeService service = new(DbContext);

            FixedCharge fixedCharge = new()
            {
                UserId = userId,
                Name = "Spotify",
                CategoryId = category.Id,
                Amount = 10.99m,
                Frequency = "Monthly",
                DayOfMonth = 8,
                StartDate = DateTime.Today
            };

            var result = await service.CreateFixedChargeAsync(fixedCharge);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);

            FixedCharge? persistedFixedCharge = DbContext.GetFixedChargeById(result.Data.Id, userId);

            Assert.IsNotNull(persistedFixedCharge);
            Assert.AreEqual("Spotify", persistedFixedCharge.Name);
        }
    }
}
