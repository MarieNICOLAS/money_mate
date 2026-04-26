using MoneyMate.Models;
using SQLite;

namespace MoneyMate.Data.Context
{
    public sealed class MoneyMateDbContext : IMoneyMateDbContext, IDisposable
    {
        private const string DemoUserEmail = "demo@moneymate.fr";
        private const string DemoUserPasswordHash = "$2a$11$X8zrnOfReIlonKzWevCyG.KLPFtTyHKM2sjphReMlX5G0xo7c2/wS";

        private readonly string _dbPath;
        private readonly object _dbLock = new();

        private SQLiteConnection? _connection;

        public MoneyMateDbContext(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("Le chemin de la base de données est requis.", nameof(dbPath));

            _dbPath = dbPath;

            string? directoryPath = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        public SQLiteConnection GetConnectionSafe()
        {
            lock (_dbLock)
            {
                return GetOrCreateConnection();
            }
        }

        public void EnsureCreated()
            => Execute(connection => 0);

        public List<User> GetUsers()
            => Execute(connection => connection.Table<User>()
                .OrderByDescending(user => user.CreatedAt)
                .ToList());

        public User? GetUserById(int id)
            => id <= 0
                ? null
                : Execute(connection => connection.Table<User>()
                    .FirstOrDefault(user => user.Id == id));

        public User? GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            string normalizedEmail = email.Trim().ToLowerInvariant();

            return Execute(connection => connection.Table<User>()
                .FirstOrDefault(user => user.Email.ToLower() == normalizedEmail));
        }

        public int InsertUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.PasswordHash))
                return 0;

            user.Email = user.Email.Trim().ToLowerInvariant();

            return ExecuteSafe(connection =>
            {
                int insertedRows = connection.Insert(user);
                return insertedRows == 1 ? user.Id : 0;
            });
        }

        public int UpdateUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.Id <= 0)
                return 0;

            user.Email = user.Email.Trim().ToLowerInvariant();

            return ExecuteSafe(connection => connection.Update(user));
        }

        public List<Category> GetCategories()
            => Execute(connection => connection.Table<Category>()
                .Where(category => category.IsSystem && category.IsActive)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToList());

        public List<Category> GetCategoriesByUserId(int userId)
        {
            if (userId <= 0)
                return [];

            return Execute(connection => connection.Query<Category>(
                """
                SELECT c.*
                FROM Categories c
                WHERE c.IsActive = 1
                  AND (
                    (c.IsSystem = 1 AND NOT EXISTS (
                        SELECT 1
                        FROM Categories o
                        WHERE o.UserId = ? AND o.ParentCategoryId = c.Id
                    ))
                    OR c.UserId = ?
                  )
                ORDER BY c.DisplayOrder, c.Name
                """,
                userId,
                userId));
        }

        public List<Category> GetAllCategoriesByUserId(int userId)
        {
            if (userId <= 0)
                return [];

            return Execute(connection => connection.Query<Category>(
                """
                SELECT c.*
                FROM Categories c
                WHERE (c.IsSystem = 1 AND NOT EXISTS (
                        SELECT 1
                        FROM Categories o
                        WHERE o.UserId = ? AND o.ParentCategoryId = c.Id
                    ))
                   OR c.UserId = ?
                ORDER BY c.DisplayOrder, c.Name
                """,
                userId,
                userId));
        }

        public List<Category> GetCustomCategoriesByUserId(int userId)
        {
            if (userId <= 0)
                return [];

            return Execute(connection => connection.Table<Category>()
                .Where(category => !category.IsSystem && category.UserId == userId)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToList());
        }

        public Category? GetCategoryById(int id)
            => id <= 0
                ? null
                : Execute(connection => connection.Table<Category>()
                    .FirstOrDefault(category => category.Id == id));

        public Category? GetCategoryById(int id, int userId)
        {
            if (id <= 0 || userId <= 0)
                return null;

            return Execute(connection => connection.Table<Category>()
                .FirstOrDefault(category => category.Id == id && (category.IsSystem || category.UserId == userId)));
        }

        public Category? GetCategoryOverride(int userId, int parentCategoryId)
        {
            if (userId <= 0 || parentCategoryId <= 0)
                return null;

            return Execute(connection => connection.Table<Category>()
                .FirstOrDefault(category =>
                    !category.IsSystem &&
                    category.UserId == userId &&
                    category.ParentCategoryId == parentCategoryId));
        }

        public int InsertCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (!category.IsSystem && category.UserId <= 0)
                return 0;

            if (string.IsNullOrWhiteSpace(category.Name))
                return 0;

            category.Name = category.Name.Trim();
            category.Color = NormalizeColor(category.Color);

            return ExecuteSafe(connection =>
            {
                int insertedRows = connection.Insert(category);
                return insertedRows == 1 ? category.Id : 0;
            });
        }

        public int UpdateCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.Id <= 0)
                return 0;

            Category? existingCategory = GetCategoryById(category.Id);
            if (existingCategory is null)
                return 0;

            if (existingCategory.IsSystem)
                return 0;

            category.Color = NormalizeColor(category.Color);

            return ExecuteSafe(connection => connection.Update(category));
        }

        public int DeleteCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.Id <= 0 || category.UserId.GetValueOrDefault() <= 0)
                return 0;

            return Execute(connection =>
            {
                Category? existingCategory = connection.Table<Category>()
                    .FirstOrDefault(item => item.Id == category.Id && item.UserId == category.UserId && !item.IsSystem);

                if (existingCategory is null)
                    return 0;

                connection.RunInTransaction(() =>
                {
                    List<int> budgetIds = connection.Table<Budget>()
                        .Where(budget => budget.UserId == category.UserId && budget.CategoryId == category.Id)
                        .Select(budget => budget.Id)
                        .ToList();

                    foreach (int budgetId in budgetIds)
                    {
                        connection.Table<AlertThreshold>()
                            .Where(alert => alert.BudgetId == budgetId)
                            .Delete();
                    }

                    connection.Table<AlertThreshold>()
                        .Where(alert => alert.UserId == category.UserId && alert.CategoryId == category.Id)
                        .Delete();

                    connection.Table<Expense>()
                        .Where(expense => expense.UserId == category.UserId && expense.CategoryId == category.Id)
                        .Delete();

                    connection.Table<FixedCharge>()
                        .Where(fixedCharge => fixedCharge.UserId == category.UserId && fixedCharge.CategoryId == category.Id)
                        .Delete();

                    connection.Table<Budget>()
                        .Where(budget => budget.UserId == category.UserId && budget.CategoryId == category.Id)
                        .Delete();

                    connection.Delete(existingCategory);
                });

                return 1;
            });
        }

        public int MigrateCategoryUsageForUser(int userId, int sourceCategoryId, int targetCategoryId)
        {
            if (userId <= 0 || sourceCategoryId <= 0 || targetCategoryId <= 0 || sourceCategoryId == targetCategoryId)
                return 0;

            return Execute(connection =>
            {
                int updatedRows = 0;

                connection.RunInTransaction(() =>
                {
                    updatedRows += connection.Execute(
                        "UPDATE Expenses SET CategoryId = ? WHERE CategoryId = ? AND UserId = ?",
                        targetCategoryId,
                        sourceCategoryId,
                        userId);

                    updatedRows += connection.Execute(
                        "UPDATE FixedCharges SET CategoryId = ? WHERE CategoryId = ? AND UserId = ?",
                        targetCategoryId,
                        sourceCategoryId,
                        userId);

                    updatedRows += connection.Execute(
                        "UPDATE Budgets SET CategoryId = ? WHERE CategoryId = ? AND UserId = ?",
                        targetCategoryId,
                        sourceCategoryId,
                        userId);

                    updatedRows += connection.Execute(
                        "UPDATE AlertThresholds SET CategoryId = ? WHERE CategoryId = ? AND UserId = ?",
                        targetCategoryId,
                        sourceCategoryId,
                        userId);
                });

                return updatedRows;
            });
        }

        public List<Expense> GetExpensesByUserId(int userId)
        {
            if (userId <= 0)
                return [];

            return Execute(connection => connection.Table<Expense>()
                .Where(expense => expense.UserId == userId)
                .OrderByDescending(expense => expense.DateOperation)
                .ToList());
        }

        public List<Expense> GetExpensesByCategory(int userId, int categoryId)
        {
            if (userId <= 0 || categoryId <= 0)
                return [];

            return Execute(connection => connection.Table<Expense>()
                .Where(expense => expense.UserId == userId && expense.CategoryId == categoryId)
                .OrderByDescending(expense => expense.DateOperation)
                .ToList());
        }

        public Expense? GetExpenseById(int id)
            => id <= 0
                ? null
                : Execute(connection => connection.Table<Expense>()
                    .FirstOrDefault(expense => expense.Id == id));

        public Expense? GetExpenseById(int id, int userId)
        {
            if (id <= 0 || userId <= 0)
                return null;

            return Execute(connection => connection.Table<Expense>()
                .FirstOrDefault(expense => expense.Id == id && expense.UserId == userId));
        }

        public int InsertExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (expense.UserId <= 0 || expense.CategoryId <= 0 || !IsCategoryAccessible(expense.CategoryId, expense.UserId))
                return 0;

            return ExecuteSafe(connection =>
            {
                int insertedRows = connection.Insert(expense);
                return insertedRows == 1 ? expense.Id : 0;
            });
        }

        public int UpdateExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (expense.Id <= 0 || expense.UserId <= 0 || !IsCategoryAccessible(expense.CategoryId, expense.UserId))
                return 0;

            Expense? existingExpense = GetExpenseById(expense.Id, expense.UserId);
            if (existingExpense is null)
                return 0;

            return ExecuteSafe(connection => connection.Update(expense));
        }

        public int DeleteExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (expense.Id <= 0 || expense.UserId <= 0)
                return 0;

            return Execute(connection =>
            {
                Expense? existingExpense = connection.Table<Expense>()
                    .FirstOrDefault(item => item.Id == expense.Id && item.UserId == expense.UserId);

                return existingExpense is null ? 0 : connection.Delete(existingExpense);
            });
        }

        public List<Budget> GetBudgetsByUserId(int userId)
        {
            if (userId <= 0)
                return [];

            return Execute(connection => connection.Table<Budget>()
                .Where(budget => budget.UserId == userId)
                .OrderByDescending(budget => budget.StartDate)
                .ToList());
        }

        public Budget? GetBudgetById(int id)
            => id <= 0
                ? null
                : Execute(connection => connection.Table<Budget>()
                    .FirstOrDefault(budget => budget.Id == id));

        public Budget? GetBudgetById(int id, int userId)
        {
            if (id <= 0 || userId <= 0)
                return null;

            return Execute(connection => connection.Table<Budget>()
                .FirstOrDefault(budget => budget.Id == id && budget.UserId == userId));
        }

        public int InsertBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (budget.UserId <= 0 || budget.Amount <= 0)
                return 0;

            if (budget.CategoryId > 0 && !IsCategoryAccessible(budget.CategoryId, budget.UserId))
                return 0;

            return ExecuteSafe(connection =>
            {
                int insertedRows = connection.Insert(budget);
                return insertedRows == 1 ? budget.Id : 0;
            });
        }

        public int UpdateBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (budget.Id <= 0 || budget.UserId <= 0 || budget.Amount <= 0)
                return 0;

            if (budget.CategoryId > 0 && !IsCategoryAccessible(budget.CategoryId, budget.UserId))
                return 0;

            Budget? existingBudget = GetBudgetById(budget.Id, budget.UserId);
            if (existingBudget is null)
                return 0;

            return ExecuteSafe(connection => connection.Update(budget));
        }

        public int DeleteBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (budget.Id <= 0 || budget.UserId <= 0)
                return 0;

            return Execute(connection =>
            {
                Budget? existingBudget = connection.Table<Budget>()
                    .FirstOrDefault(item => item.Id == budget.Id && item.UserId == budget.UserId);

                if (existingBudget is null)
                    return 0;

                connection.RunInTransaction(() =>
                {
                    connection.Table<AlertThreshold>()
                        .Where(alert => alert.BudgetId == budget.Id)
                        .Delete();

                    connection.Delete(existingBudget);
                });

                return 1;
            });
        }

        public List<FixedCharge> GetFixedChargesByUserId(int userId)
        {
            if (userId <= 0)
                return [];

            return Execute(connection => connection.Table<FixedCharge>()
                .Where(fixedCharge => fixedCharge.UserId == userId)
                .OrderByDescending(fixedCharge => fixedCharge.CreatedAt)
                .ToList());
        }

        public FixedCharge? GetFixedChargeById(int id)
            => id <= 0
                ? null
                : Execute(connection => connection.Table<FixedCharge>()
                    .FirstOrDefault(fixedCharge => fixedCharge.Id == id));

        public FixedCharge? GetFixedChargeById(int id, int userId)
        {
            if (id <= 0 || userId <= 0)
                return null;

            return Execute(connection => connection.Table<FixedCharge>()
                .FirstOrDefault(fixedCharge => fixedCharge.Id == id && fixedCharge.UserId == userId));
        }

        public int InsertFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (fixedCharge.UserId <= 0 || fixedCharge.CategoryId <= 0 || !IsCategoryAccessible(fixedCharge.CategoryId, fixedCharge.UserId))
                return 0;

            return ExecuteSafe(connection =>
            {
                int insertedRows = connection.Insert(fixedCharge);
                return insertedRows == 1 ? fixedCharge.Id : 0;
            });
        }

        public int UpdateFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (fixedCharge.Id <= 0 || fixedCharge.UserId <= 0 || !IsCategoryAccessible(fixedCharge.CategoryId, fixedCharge.UserId))
                return 0;

            FixedCharge? existingFixedCharge = GetFixedChargeById(fixedCharge.Id, fixedCharge.UserId);
            if (existingFixedCharge is null)
                return 0;

            return ExecuteSafe(connection => connection.Update(fixedCharge));
        }

        public int DeleteFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (fixedCharge.Id <= 0 || fixedCharge.UserId <= 0)
                return 0;

            return Execute(connection =>
            {
                FixedCharge? existingFixedCharge = connection.Table<FixedCharge>()
                    .FirstOrDefault(item => item.Id == fixedCharge.Id && item.UserId == fixedCharge.UserId);

                return existingFixedCharge is null ? 0 : connection.Delete(existingFixedCharge);
            });
        }

        public int GetActiveFixedChargesCountByUserId(int userId)
            => userId <= 0
                ? 0
                : Execute(connection => connection.Table<FixedCharge>()
                    .Count(fixedCharge => fixedCharge.UserId == userId && fixedCharge.IsActive));

        public List<AlertThreshold> GetAlertThresholdsByUserId(int userId)
        {
            if (userId <= 0)
                return [];

            return Execute(connection => connection.Table<AlertThreshold>()
                .Where(alert => alert.UserId == userId)
                .OrderByDescending(alert => alert.CreatedAt)
                .ToList());
        }

        public AlertThreshold? GetAlertThresholdById(int id)
            => id <= 0
                ? null
                : Execute(connection => connection.Table<AlertThreshold>()
                    .FirstOrDefault(alert => alert.Id == id));

        public AlertThreshold? GetAlertThresholdById(int id, int userId)
        {
            if (id <= 0 || userId <= 0)
                return null;

            return Execute(connection => connection.Table<AlertThreshold>()
                .FirstOrDefault(alert => alert.Id == id && alert.UserId == userId));
        }

        public int InsertAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (!IsAlertThresholdValid(alertThreshold))
                return 0;

            return ExecuteSafe(connection =>
            {
                int insertedRows = connection.Insert(alertThreshold);
                return insertedRows == 1 ? alertThreshold.Id : 0;
            });
        }

        public int UpdateAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (alertThreshold.Id <= 0 || !IsAlertThresholdValid(alertThreshold))
                return 0;

            AlertThreshold? existingAlert = GetAlertThresholdById(alertThreshold.Id, alertThreshold.UserId);
            if (existingAlert is null)
                return 0;

            return ExecuteSafe(connection => connection.Update(alertThreshold));
        }

        public int DeleteAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (alertThreshold.Id <= 0 || alertThreshold.UserId <= 0)
                return 0;

            return Execute(connection =>
            {
                AlertThreshold? existingAlert = connection.Table<AlertThreshold>()
                    .FirstOrDefault(item => item.Id == alertThreshold.Id && item.UserId == alertThreshold.UserId);

                return existingAlert is null ? 0 : connection.Delete(existingAlert);
            });
        }

        public void DeleteAllUserData(int userId)
        {
            if (userId <= 0)
                return;

            Execute(connection =>
            {
                connection.RunInTransaction(() =>
                {
                    connection.Table<AlertThreshold>()
                        .Where(alert => alert.UserId == userId)
                        .Delete();

                    connection.Table<Expense>()
                        .Where(expense => expense.UserId == userId)
                        .Delete();

                    connection.Table<FixedCharge>()
                        .Where(fixedCharge => fixedCharge.UserId == userId)
                        .Delete();

                    connection.Table<Budget>()
                        .Where(budget => budget.UserId == userId)
                        .Delete();

                    connection.Table<Category>()
                        .Where(category => !category.IsSystem && category.UserId == userId)
                        .Delete();

                    User? user = connection.Table<User>()
                        .FirstOrDefault(item => item.Id == userId);

                    if (user is not null)
                        connection.Delete(user);
                });

                return 0;
            });
        }

        public void Close()
        {
            lock (_dbLock)
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
            }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        private SQLiteConnection GetOrCreateConnection()
        {
            if (_connection != null)
                return _connection;

            _connection = new SQLiteConnection(
                _dbPath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.FullMutex);

            ConfigureDatabase(_connection);
            InitializeDatabase(_connection);

            return _connection;
        }

        private static void ConfigureDatabase(SQLiteConnection connection)
        {
            connection.ExecuteScalar<string>("PRAGMA journal_mode = WAL;");
            connection.Execute("PRAGMA foreign_keys = ON;");
            connection.Execute("PRAGMA synchronous = NORMAL;");
        }

        private static void InitializeDatabase(SQLiteConnection connection)
        {
            connection.CreateTable<User>();
            connection.CreateTable<Category>();
            connection.CreateTable<Expense>();
            connection.CreateTable<Budget>();
            connection.CreateTable<FixedCharge>();
            connection.CreateTable<AlertThreshold>();

            EnsureBudgetCategoryColumn(connection);
            EnsureCategoryOverrideColumns(connection);
            CreateIndexes(connection);
            SeedCategories(connection);
            SeedDemoData(connection);
        }

        private static void EnsureBudgetCategoryColumn(SQLiteConnection connection)
        {
            bool hasCategoryIdColumn = connection.GetTableInfo("Budgets")
                .Any(column => string.Equals(column.Name, nameof(Budget.CategoryId), StringComparison.OrdinalIgnoreCase));

            if (!hasCategoryIdColumn)
                connection.Execute("ALTER TABLE Budgets ADD COLUMN CategoryId INTEGER NOT NULL DEFAULT 0;");
        }

        private static void EnsureCategoryOverrideColumns(SQLiteConnection connection)
        {
            bool hasParentCategoryIdColumn = connection.GetTableInfo("Categories")
                .Any(column => string.Equals(column.Name, nameof(Category.ParentCategoryId), StringComparison.OrdinalIgnoreCase));

            if (!hasParentCategoryIdColumn)
                connection.Execute("ALTER TABLE Categories ADD COLUMN ParentCategoryId INTEGER NULL;");
        }

        private static void CreateIndexes(SQLiteConnection connection)
        {
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Users_Email ON Users(Email)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Expenses_User_Date ON Expenses(UserId, DateOperation DESC)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Expenses_Category ON Expenses(CategoryId)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Categories_User_IsSystem ON Categories(UserId, IsSystem)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Categories_User_Parent ON Categories(UserId, ParentCategoryId)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Categories_Parent ON Categories(ParentCategoryId)");
            connection.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_Categories_User_ParentOverride ON Categories(UserId, ParentCategoryId) WHERE ParentCategoryId IS NOT NULL");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Budgets_User_StartDate ON Budgets(UserId, StartDate DESC)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_FixedCharges_User_Active ON FixedCharges(UserId, IsActive)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_AlertThresholds_User ON AlertThresholds(UserId)");
        }

        private static void SeedCategories(SQLiteConnection connection)
        {
            List<Category> existingSystemCategories = connection.Table<Category>()
                .Where(category => category.IsSystem)
                .ToList();

            Dictionary<string, Category> existingByName = existingSystemCategories
                .GroupBy(category => category.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            List<Category> expectedCategories =
            [
                new() { Name = "Alimentation", Description = "Courses, boulangerie et repas du quotidien.", Color = "#F4B183", Icon = "🛒", DisplayOrder = 1, IsSystem = true, IsActive = true },
                new() { Name = "Logement", Description = "Loyer, énergie et dépenses liées au logement.", Color = "#D9E2F3", Icon = "🏠", DisplayOrder = 2, IsSystem = true, IsActive = true },
                new() { Name = "Transport", Description = "Essence, transports et mobilité.", Color = "#8EC9D3", Icon = "🚗", DisplayOrder = 3, IsSystem = true, IsActive = true },
                new() { Name = "Santé", Description = "Médecin, pharmacie et bien-être.", Color = "#F8CBAD", Icon = "💊", DisplayOrder = 4, IsSystem = true, IsActive = true },
                new() { Name = "Loisirs", Description = "Sorties, culture et moments de détente.", Color = "#7C92C3", Icon = "🎉", DisplayOrder = 5, IsSystem = true, IsActive = true },
                new() { Name = "Abonnements", Description = "Internet, streaming et services récurrents.", Color = "#B4A7D6", Icon = "📺", DisplayOrder = 6, IsSystem = true, IsActive = true },
                new() { Name = "Famille & cadeaux", Description = "Anniversaires, fêtes et cadeaux.", Color = "#F4CCCC", Icon = "🎁", DisplayOrder = 7, IsSystem = true, IsActive = true },
                new() { Name = "Vie quotidienne", Description = "Maison, entretien et achats utiles du quotidien.", Color = "#CFE2F3", Icon = "🧺", DisplayOrder = 8, IsSystem = true, IsActive = true }
            ];

            connection.RunInTransaction(() =>
            {
                foreach (Category expectedCategory in expectedCategories)
                {
                    if (existingByName.TryGetValue(expectedCategory.Name, out Category? existingCategory))
                    {
                        existingCategory.Description = expectedCategory.Description;
                        existingCategory.Color = expectedCategory.Color;
                        existingCategory.Icon = expectedCategory.Icon;
                        existingCategory.DisplayOrder = expectedCategory.DisplayOrder;
                        existingCategory.IsActive = true;
                        existingCategory.IsSystem = true;
                        existingCategory.UserId = null;
                        existingCategory.ParentCategoryId = null;
                        connection.Update(existingCategory);
                        continue;
                    }

                    connection.Insert(expectedCategory);
                }
            });
        }

        private static void SeedDemoData(SQLiteConnection connection)
        {
            User demoUser = EnsureDemoUser(connection);
            if (demoUser.Id <= 0)
                return;

            bool hasExistingDemoData = connection.Table<Budget>().Any(budget => budget.UserId == demoUser.Id)
                || connection.Table<Expense>().Any(expense => expense.UserId == demoUser.Id)
                || connection.Table<FixedCharge>().Any(fixedCharge => fixedCharge.UserId == demoUser.Id);

            if (hasExistingDemoData)
                return;

            Dictionary<string, Category> categoriesByName = connection.Table<Category>()
                .Where(category => category.IsSystem && category.IsActive)
                .ToDictionary(category => category.Name, category => category, StringComparer.OrdinalIgnoreCase);

            string[] requiredCategoryNames =
            [
                "Alimentation",
                "Logement",
                "Transport",
                "Santé",
                "Loisirs",
                "Abonnements",
                "Famille & cadeaux",
                "Vie quotidienne"
            ];

            if (requiredCategoryNames.Any(name => !categoriesByName.ContainsKey(name)))
                return;

            DateTime today = DateTime.Today;
            DateTime firstSeedMonth = new(today.Year - 1, 12, 1);
            DateTime lastSeedMonth = today.Month >= 4
                ? new(today.Year, 4, 1)
                : new(today.Year, today.Month, 1);

            if (lastSeedMonth < firstSeedMonth)
                return;

            List<DateTime> seededMonths = [];
            for (DateTime month = firstSeedMonth; month <= lastSeedMonth; month = month.AddMonths(1))
                seededMonths.Add(month);

            List<(string Name, string Description, decimal Amount, string CategoryName, int DayOfMonth)> fixedChargeDefinitions =
            [
                ("Loyer", "Appartement T2 en périphérie", 690m, "Logement", 5),
                ("Électricité", "Prélèvement énergie", 62.50m, "Logement", 12),
                ("Fibre", "Abonnement internet maison", 29.99m, "Abonnements", 7),
                ("Forfait mobile", "Téléphone mobile", 15.99m, "Abonnements", 18),
                ("Netflix", "Abonnement streaming", 13.49m, "Abonnements", 22),
                ("Assurance auto", "Cotisation mensuelle auto", 58.90m, "Transport", 14)
            ];

            List<Budget> budgets = seededMonths
                .Select(month => CreateDemoBudget(demoUser.Id, month))
                .ToList();

            List<FixedCharge> fixedCharges = fixedChargeDefinitions
                .Select(definition => new FixedCharge
                {
                    UserId = demoUser.Id,
                    Name = definition.Name,
                    Description = definition.Description,
                    Amount = definition.Amount,
                    CategoryId = categoriesByName[definition.CategoryName].Id,
                    Frequency = "Monthly",
                    DayOfMonth = definition.DayOfMonth,
                    StartDate = firstSeedMonth,
                    EndDate = null,
                    IsActive = true,
                    AutoCreateExpense = true,
                    CreatedAt = firstSeedMonth.AddDays(definition.DayOfMonth - 1)
                })
                .ToList();

            List<Expense> expenses = [];
            foreach (DateTime month in seededMonths)
            {
                expenses.AddRange(fixedChargeDefinitions.Select(definition => new Expense
                {
                    UserId = demoUser.Id,
                    CategoryId = categoriesByName[definition.CategoryName].Id,
                    Amount = definition.Amount,
                    Note = definition.Name,
                    DateOperation = CreateDemoDate(month, definition.DayOfMonth),
                    IsFixedCharge = true
                }));

                foreach ((int Day, decimal Amount, string CategoryName, string Note) expense in GetDemoMonthlyExpenses(month))
                {
                    expenses.Add(new Expense
                    {
                        UserId = demoUser.Id,
                        CategoryId = categoriesByName[expense.CategoryName].Id,
                        Amount = expense.Amount,
                        Note = expense.Note,
                        DateOperation = CreateDemoDate(month, expense.Day),
                        IsFixedCharge = false
                    });
                }
            }

            connection.RunInTransaction(() =>
            {
                connection.InsertAll(budgets);
                connection.InsertAll(fixedCharges);
                connection.InsertAll(expenses);
            });
        }

        private static User EnsureDemoUser(SQLiteConnection connection)
        {
            User? existingUser = connection.Table<User>()
                .FirstOrDefault(user => user.Email.ToLower() == DemoUserEmail);

            if (existingUser is not null)
                return existingUser;

            User demoUser = new()
            {
                Email = DemoUserEmail,
                PasswordHash = DemoUserPasswordHash,
                Devise = "EUR",
                BudgetStartDay = 1,
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            connection.Insert(demoUser);
            return demoUser;
        }

        private static Budget CreateDemoBudget(int userId, DateTime month)
        {
            decimal amount = month.Month switch
            {
                12 => 2125m,
                1 => 1810m,
                2 => 1825m,
                3 => 1835m,
                4 => 1855m,
                _ => 1810m
            };

            DateTime monthStart = new(month.Year, month.Month, 1);

            return new Budget
            {
                UserId = userId,
                Amount = amount,
                PeriodType = "Monthly",
                StartDate = monthStart,
                EndDate = monthStart.AddMonths(1).AddDays(-1),
                IsActive = true,
                CreatedAt = monthStart.AddDays(1),
                CategoryId = 0
            };
        }

        private static IEnumerable<(int Day, decimal Amount, string CategoryName, string Note)> GetDemoMonthlyExpenses(DateTime month)
            => month.Month switch
            {
                12 =>
                [
                    (6, 245.30m, "Alimentation", "Courses de décembre et produits pour les fêtes"),
                    (9, 41.80m, "Vie quotidienne", "Produits maison et déco de saison"),
                    (12, 96.40m, "Transport", "Plein d'essence avant les trajets familiaux"),
                    (14, 279.90m, "Famille & cadeaux", "Cadeaux de Noël pour la famille"),
                    (21, 52.00m, "Loisirs", "Marché de Noël et sortie en ville"),
                    (24, 118.60m, "Alimentation", "Repas de Noël en famille"),
                    (27, 23.40m, "Santé", "Pharmacie hiver"),
                    (30, 64.50m, "Famille & cadeaux", "Étrennes et petits cadeaux de fin d'année")
                ],
                1 =>
                [
                    (4, 218.45m, "Alimentation", "Courses de reprise après les fêtes"),
                    (8, 18.90m, "Santé", "Pharmacie pour rhume hivernal"),
                    (11, 105.70m, "Transport", "Plein d'essence après la hausse des prix"),
                    (18, 79.00m, "Famille & cadeaux", "Cadeau d'anniversaire pour maman"),
                    (22, 26.40m, "Vie quotidienne", "Entretien de la maison"),
                    (25, 34.20m, "Loisirs", "Restaurant simple du week-end")
                ],
                2 =>
                [
                    (3, 226.10m, "Alimentation", "Courses du mois"),
                    (10, 111.80m, "Transport", "Essence avec inflation persistante"),
                    (14, 46.30m, "Loisirs", "Soirée de Saint-Valentin à la maison"),
                    (16, 55.00m, "Famille & cadeaux", "Cadeau d'anniversaire pour un ami proche"),
                    (21, 29.90m, "Vie quotidienne", "Lessive et entretien courant"),
                    (24, 24.00m, "Loisirs", "Cinéma du dimanche")
                ],
                3 =>
                [
                    (3, 232.75m, "Alimentation", "Courses du mois"),
                    (8, 109.40m, "Transport", "Carburant pour trajets travail et famille"),
                    (12, 89.00m, "Famille & cadeaux", "Cadeau d'anniversaire pour le frère"),
                    (17, 62.50m, "Alimentation", "Repas familial du dimanche"),
                    (22, 67.20m, "Vie quotidienne", "Chaussures et achats utiles du printemps"),
                    (28, 26.00m, "Santé", "Consultation médecin généraliste")
                ],
                4 =>
                [
                    (4, 238.20m, "Alimentation", "Courses du mois"),
                    (9, 116.30m, "Transport", "Essence après la hausse liée au détroit d'Ormuz"),
                    (12, 38.00m, "Famille & cadeaux", "Chocolats de Pâques pour les enfants"),
                    (20, 92.40m, "Alimentation", "Repas familial de Pâques"),
                    (23, 27.50m, "Loisirs", "Sortie parc et café"),
                    (26, 31.80m, "Vie quotidienne", "Produits ménagers et rangement de printemps")
                ],
                _ => []
            };

        private static DateTime CreateDemoDate(DateTime month, int day)
        {
            int safeDay = Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month));
            return new DateTime(month.Year, month.Month, safeDay, 12, 0, 0);
        }

        private TResult Execute<TResult>(Func<SQLiteConnection, TResult> action)
        {
            lock (_dbLock)
            {
                return action(GetOrCreateConnection());
            }
        }

        private int ExecuteSafe(Func<SQLiteConnection, int> action)
        {
            try
            {
                return Execute(action);
            }
            catch (SQLiteException)
            {
                return 0;
            }
        }

        private bool IsCategoryAccessible(int categoryId, int userId)
            => Execute(connection => connection.ExecuteScalar<int>(
                """
                SELECT COUNT(1)
                FROM Categories c
                WHERE c.Id = ?
                  AND c.IsActive = 1
                  AND (
                    c.UserId = ?
                    OR (
                        c.IsSystem = 1
                        AND NOT EXISTS (
                            SELECT 1
                            FROM Categories o
                            WHERE o.UserId = ? AND o.ParentCategoryId = c.Id
                        )
                    )
                  )
                """,
                categoryId,
                userId,
                userId) > 0);

        private bool IsAlertThresholdValid(AlertThreshold alertThreshold)
        {
            if (alertThreshold.UserId <= 0 || alertThreshold.ThresholdPercentage < 0)
                return false;

            if (alertThreshold.CategoryId.HasValue && !IsCategoryAccessible(alertThreshold.CategoryId.Value, alertThreshold.UserId))
                return false;

            if (alertThreshold.BudgetId.HasValue && GetBudgetById(alertThreshold.BudgetId.Value, alertThreshold.UserId) is null)
                return false;

            return true;
        }

        private static string NormalizeColor(string color)
            => string.IsNullOrWhiteSpace(color) ? "#6B7A8F" : color.Trim();
    }
}
