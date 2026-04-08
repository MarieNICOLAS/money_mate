using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace IntegrationTests.Services
{
    [TestClass]
    public class ExpenseServiceIntegrationTests : TestDatabaseFixture
    {
        [TestMethod]
        public async Task CreateExpenseAsync_WithValidExpense_PersistsExpense()
        {
            int userId = CreateUser();
            Category category = CreateCategory(userId);

            ExpenseService service = new(DbContext);

            Expense expense = new()
            {
                UserId = userId,
                CategoryId = category.Id,
                Amount = 23.90m,
                Note = "  Dépense test  ",
                DateOperation = DateTime.Now.AddMinutes(-1)
            };

            var result = await service.CreateExpenseAsync(expense);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Data);

            Expense? persistedExpense = DbContext.GetExpenseById(result.Data.Id, userId);

            Assert.IsNotNull(persistedExpense);
            Assert.AreEqual("Dépense test", persistedExpense.Note);
            Assert.AreEqual(23.90m, persistedExpense.Amount);
        }

        [TestMethod]
        public async Task CreateExpenseAsync_WithInactiveCategory_ReturnsCategoryNotFound()
        {
            int userId = CreateUser();
            Category category = CreateCategory(userId, isActive: false);

            ExpenseService service = new(DbContext);

            Expense expense = new()
            {
                UserId = userId,
                CategoryId = category.Id,
                Amount = 9.99m,
                DateOperation = DateTime.Now.AddMinutes(-1)
            };

            var result = await service.CreateExpenseAsync(expense);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("EXPENSE_CATEGORY_NOT_FOUND", result.ErrorCode);
        }
    }
}
