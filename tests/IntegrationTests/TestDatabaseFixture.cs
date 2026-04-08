using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoneyMate.Data.Context;
using MoneyMate.Models;

namespace IntegrationTests
{
    /// <summary>
    /// Base commune pour les tests d'intégration avec SQLite temporaire.
    /// </summary>
    public abstract class TestDatabaseFixture
    {
        private string _databaseDirectory = string.Empty;

        protected MoneyMateDbContext DbContext { get; private set; } = null!;

        [TestInitialize]
        public void InitializeDatabase()
        {
            _databaseDirectory = Path.Combine(
                Path.GetTempPath(),
                "MoneyMate.Tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_databaseDirectory);

            string databasePath = Path.Combine(_databaseDirectory, "MoneyMate.tests.db3");
            DbContext = new MoneyMateDbContext(databasePath);
        }

        [TestCleanup]
        public void CleanupDatabase()
        {
            DbContext.Close();

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (Directory.Exists(_databaseDirectory))
                        Directory.Delete(_databaseDirectory, true);

                    return;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                }
                catch (UnauthorizedAccessException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                }
            }
        }

        protected int CreateUser(string? email = null)
        {
            User user = new()
            {
                Email = email ?? $"user-{Guid.NewGuid():N}@tests.local",
                PasswordHash = "hash",
                Devise = "EUR",
                BudgetStartDay = 1,
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            return DbContext.InsertUser(user);
        }

        protected Category CreateCategory(int userId, bool isActive = true, string? name = null)
        {
            Category category = new()
            {
                UserId = userId,
                IsSystem = false,
                Name = name ?? $"Cat-{Guid.NewGuid():N}",
                Description = "Catégorie de test",
                Color = "#123456",
                Icon = "tag",
                DisplayOrder = 1,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };

            category.Id = DbContext.InsertCategory(category);
            return category;
        }

        protected Budget CreateBudget(int userId, int categoryId, DateTime startDate, DateTime? endDate = null, decimal amount = 100m)
        {
            Budget budget = new()
            {
                UserId = userId,
                CategoryId = categoryId,
                Amount = amount,
                PeriodType = "Monthly",
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            budget.Id = DbContext.InsertBudget(budget);
            return budget;
        }
    }
}
