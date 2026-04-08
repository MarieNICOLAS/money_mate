using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace UnitTests.Services
{
    [TestClass]
    public class BudgetServiceTests
    {
        [TestMethod]
        public async Task CreateBudgetAsync_WithUnknownCategory_ReturnsCategoryNotFound()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetCategoryById(3, 1))
                .Returns((Category?)null);

            BudgetService service = new(dbContextMock.Object);

            Budget budget = new()
            {
                UserId = 1,
                CategoryId = 3,
                Amount = 200m,
                PeriodType = "Monthly",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1)
            };

            var result = await service.CreateBudgetAsync(budget);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("BUDGET_CATEGORY_NOT_FOUND", result.ErrorCode);
        }

        [TestMethod]
        public async Task CreateBudgetAsync_WithOverlappingBudget_ReturnsConflict()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetCategoryById(3, 1))
                .Returns(new Category
                {
                    Id = 3,
                    UserId = 1,
                    Name = "Courses",
                    IsActive = true
                });

            dbContextMock.Setup(x => x.GetBudgetsByUserId(1))
                .Returns(new List<Budget>
                {
                    new()
                    {
                        Id = 8,
                        UserId = 1,
                        CategoryId = 3,
                        Amount = 150m,
                        PeriodType = "Monthly",
                        StartDate = new DateTime(2026, 1, 1),
                        EndDate = new DateTime(2026, 1, 31),
                        IsActive = true
                    }
                });

            BudgetService service = new(dbContextMock.Object);

            Budget budget = new()
            {
                UserId = 1,
                CategoryId = 3,
                Amount = 250m,
                PeriodType = "Monthly",
                StartDate = new DateTime(2026, 1, 15),
                EndDate = new DateTime(2026, 2, 15)
            };

            var result = await service.CreateBudgetAsync(budget);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("BUDGET_CONFLICT", result.ErrorCode);
        }
    }
}
