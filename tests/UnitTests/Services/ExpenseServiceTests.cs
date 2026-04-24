using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace UnitTests.Services
{
    [TestClass]
    public class ExpenseServiceTests
    {
        [TestMethod]
        public async Task CreateExpenseAsync_WithInactiveCategory_ReturnsCategoryNotFound()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetBudgetsByUserId(1))
                .Returns(new List<Budget>
                {
                    new() { Id = 1, UserId = 1, Amount = 100m, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(27), IsActive = true }
                });

            dbContextMock.Setup(x => x.GetCategoryById(10, 1))
                .Returns(new Category
                {
                    Id = 10,
                    UserId = 1,
                    Name = "Transport",
                    IsActive = false
                });

            ExpenseService service = new(dbContextMock.Object);

            Expense expense = new()
            {
                UserId = 1,
                CategoryId = 10,
                Amount = 25.50m,
                DateOperation = DateTime.Now.AddMinutes(-1)
            };

            var result = await service.CreateExpenseAsync(expense);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("EXPENSE_CATEGORY_NOT_FOUND", result.ErrorCode);
        }

        [TestMethod]
        public async Task CreateExpenseAsync_WithValidExpense_ReturnsSuccess()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetBudgetsByUserId(1))
                .Returns(new List<Budget>
                {
                    new() { Id = 1, UserId = 1, Amount = 100m, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(27), IsActive = true }
                });

            dbContextMock.Setup(x => x.GetCategoryById(10, 1))
                .Returns(new Category
                {
                    Id = 10,
                    UserId = 1,
                    Name = "Courses",
                    IsActive = true
                });

            dbContextMock.Setup(x => x.InsertExpense(It.IsAny<Expense>()))
                .Callback<Expense>(expense => expense.Id = 42)
                .Returns(42);

            ExpenseService service = new(dbContextMock.Object);

            Expense expense = new()
            {
                UserId = 1,
                CategoryId = 10,
                Amount = 19.99m,
                Note = "  Pain et lait  ",
                DateOperation = DateTime.Now.AddMinutes(-1)
            };

            var result = await service.CreateExpenseAsync(expense);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(42, result.Data.Id);
            Assert.AreEqual("Pain et lait", result.Data.Note);
        }

        [TestMethod]
        public async Task CreateExpenseAsync_WithoutBudget_ReturnsBudgetRequired()
        {
            Mock<IMoneyMateDbContext> dbContextMock = new();

            dbContextMock.Setup(x => x.GetBudgetsByUserId(1))
                .Returns(new List<Budget>());

            ExpenseService service = new(dbContextMock.Object);

            Expense expense = new()
            {
                UserId = 1,
                CategoryId = 10,
                Amount = 19.99m,
                DateOperation = DateTime.Now.AddMinutes(-1)
            };

            var result = await service.CreateExpenseAsync(expense);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("EXPENSE_BUDGET_REQUIRED", result.ErrorCode);
        }
    }
}
