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

        [TestMethod]
        public async Task CustomizeSystemCategoryAsync_CreatesUserOverrideAndMigratesOnlyCurrentUserExpenses()
        {
            int userId = CreateUser();
            int otherUserId = CreateUser();
            Category systemCategory = DbContext.GetCategories().First();

            int userExpenseId = DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = systemCategory.Id,
                Amount = 20m,
                Note = "Avant personnalisation",
                DateOperation = DateTime.Now.AddMinutes(-1)
            });

            int otherUserExpenseId = DbContext.InsertExpense(new Expense
            {
                UserId = otherUserId,
                CategoryId = systemCategory.Id,
                Amount = 30m,
                Note = "Autre utilisateur",
                DateOperation = DateTime.Now.AddMinutes(-1)
            });

            CategoryService service = new(DbContext);

            var result = await service.CustomizeSystemCategoryAsync(new Category
            {
                Id = systemCategory.Id,
                UserId = userId,
                Name = "Courses perso",
                Description = "Version utilisateur",
                Color = "#112233",
                Icon = "tag"
            });

            Assert.IsTrue(result.IsSuccess, result.Message);
            Assert.IsNotNull(result.Data);
            Assert.IsFalse(result.Data!.IsSystem);
            Assert.AreEqual(userId, result.Data.UserId);
            Assert.AreEqual(systemCategory.Id, result.Data.ParentCategoryId);
            Assert.AreEqual("Courses perso", result.Data.Name);

            Category? unchangedSystemCategory = DbContext.GetCategoryById(systemCategory.Id);
            Assert.IsNotNull(unchangedSystemCategory);
            Assert.AreEqual(systemCategory.Name, unchangedSystemCategory!.Name);

            Expense? migratedExpense = DbContext.GetExpenseById(userExpenseId, userId);
            Expense? untouchedOtherExpense = DbContext.GetExpenseById(otherUserExpenseId, otherUserId);

            Assert.AreEqual(result.Data.Id, migratedExpense!.CategoryId);
            Assert.AreEqual(systemCategory.Id, untouchedOtherExpense!.CategoryId);

            List<Category> userCategories = DbContext.GetCategoriesByUserId(userId);
            Assert.IsFalse(userCategories.Any(category => category.Id == systemCategory.Id));
            Assert.IsTrue(userCategories.Any(category => category.Id == result.Data.Id));

            List<Category> otherUserCategories = DbContext.GetCategoriesByUserId(otherUserId);
            Assert.IsTrue(otherUserCategories.Any(category => category.Id == systemCategory.Id));

            int blockedOldCategoryExpenseId = DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = systemCategory.Id,
                Amount = 10m,
                Note = "Ancienne catégorie masquée",
                DateOperation = DateTime.Now.AddMinutes(-1)
            });

            int newCategoryExpenseId = DbContext.InsertExpense(new Expense
            {
                UserId = userId,
                CategoryId = result.Data.Id,
                Amount = 10m,
                Note = "Nouvelle catégorie visible",
                DateOperation = DateTime.Now.AddMinutes(-1)
            });

            Assert.AreEqual(0, blockedOldCategoryExpenseId);
            Assert.IsTrue(newCategoryExpenseId > 0);
        }
    }
}
