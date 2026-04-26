using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace UnitTests.Services
{
    [TestClass]
    public class CategoryServiceTests
    {
        [TestMethod]
        public async Task CreateCategoryAsync_WithDuplicateName_ReturnsFailure()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetAllCategoriesByUserId(1))
                .Returns(new List<Category>
                {
                    new()
                    {
                        Id = 1,
                        UserId = 1,
                        Name = "Courses",
                        IsActive = true
                    }
                });

            CategoryService service = new(dbContextMock.Object);

            Category category = new()
            {
                UserId = 1,
                Name = "  courses  "
            };

            var result = await service.CreateCategoryAsync(category);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("CATEGORY_NAME_ALREADY_EXISTS", result.ErrorCode);
        }

        [TestMethod]
        public async Task DeleteCategoryAsync_WhenCategoryIsInUse_ReturnsFailure()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetCategoryById(2, 1))
                .Returns(new Category
                {
                    Id = 2,
                    UserId = 1,
                    Name = "Transport",
                    IsSystem = false,
                    IsActive = true
                });

            dbContextMock.Setup(x => x.GetExpensesByCategory(1, 2))
                .Returns(new List<Expense>
                {
                    new()
                    {
                        Id = 5,
                        UserId = 1,
                        CategoryId = 2,
                        Amount = 12m,
                        DateOperation = DateTime.Now.AddMinutes(-1)
                    }
                });

            CategoryService service = new(dbContextMock.Object);

            var result = await service.DeleteCategoryAsync(2, 1);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("CATEGORY_IN_USE", result.ErrorCode);
        }
    }
}
