using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace IntegrationTests.Services
{
    [TestClass]
    public class CategoryServiceIntegrationTests : TestDatabaseFixture
    {
        [TestMethod]
        public async Task CreateCategoryAsync_WithDuplicateName_ReturnsFailure()
        {
            int userId = CreateUser();
            CreateCategory(userId, name: "Courses");

            CategoryService service = new(DbContext);

            Category category = new()
            {
                UserId = userId,
                Name = "  courses  "
            };

            var result = await service.CreateCategoryAsync(category);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("CATEGORY_NAME_ALREADY_EXISTS", result.ErrorCode);
        }

        [TestMethod]
        public async Task DeleteCategoryAsync_WhenCategoryIsUsed_ReturnsFailure()
        {
            int userId = CreateUser();
            Category category = CreateCategory(userId);

            DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = category.Id,
                Amount = 20m,
                Note = "Expense liée",
                DateOperation = DateTime.Now.AddMinutes(-1)
            });

            CategoryService service = new(DbContext);

            var result = await service.DeleteCategoryAsync(category.Id, userId);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorCode));
        }
    }
}
