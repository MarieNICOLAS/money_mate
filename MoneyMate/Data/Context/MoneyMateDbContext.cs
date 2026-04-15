using MoneyMate.Models;
using SQLite;

namespace MoneyMate.Data.Context
{
    /// <summary>
    /// Contexte SQLite pour Money Mate.
    /// Centralise les opérations CRUD synchrones, sécurise l'accès concurrent
    /// et optimise la configuration locale de SQLite.
    /// </summary>
    public sealed class MoneyMateDbContext : IMoneyMateDbContext
    {
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

        /// <summary>
        /// Retourne la connexion SQLite existante ou l'initialise si nécessaire.
        /// L'appelant doit déjà être dans une section protégée.
        /// </summary>
        private SQLiteConnection GetOrCreateConnection()
        {
            if (_connection is not null)
                return _connection;

            try
            {
                _connection = new SQLiteConnection(
                    _dbPath,
                    SQLiteOpenFlags.ReadWrite |
                    SQLiteOpenFlags.Create |
                    SQLiteOpenFlags.FullMutex);

                ConfigureDatabase(_connection);
                InitializeDatabase(_connection);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Erreur SQLite à l'initialisation de la base '{_dbPath}' : {ex}");
                throw;
            }

            return _connection;
        }

        private static void ConfigureDatabase(SQLiteConnection connection)
        {
            connection.Execute("PRAGMA foreign_keys = ON;");
            connection.Execute("PRAGMA journal_mode = WAL;");
            connection.Execute("PRAGMA synchronous = NORMAL;");
            connection.Execute("PRAGMA temp_store = MEMORY;");
        }

        private static void InitializeDatabase(SQLiteConnection connection)
        {
            connection.CreateTable<User>();
            connection.CreateTable<Category>();
            connection.CreateTable<Budget>();
            connection.CreateTable<Expense>();
            connection.CreateTable<FixedCharge>();
            connection.CreateTable<AlertThreshold>();

            EnsureSchemaUpToDate(connection);
            EnsureIndexes(connection);
            SeedDefaultCategories(connection);
        }

        private static void EnsureSchemaUpToDate(SQLiteConnection connection)
        {
            EnsureCategoriesSchema(connection);
        }

        private static void EnsureCategoriesSchema(SQLiteConnection connection)
        {
            if (!HasColumn(connection, "Categories", "UserId"))
                connection.Execute("ALTER TABLE Categories ADD COLUMN UserId INTEGER NULL");

            if (!HasColumn(connection, "Categories", "IsSystem"))
                connection.Execute("ALTER TABLE Categories ADD COLUMN IsSystem INTEGER NOT NULL DEFAULT 0");

            connection.Execute("UPDATE Categories SET IsSystem = 1 WHERE UserId IS NULL");
        }

        private static void EnsureIndexes(SQLiteConnection connection)
        {
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Users_Email ON Users(Email)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Expenses_UserId_DateOperation ON Expenses(UserId, DateOperation DESC)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Expenses_UserId_CategoryId ON Expenses(UserId, CategoryId)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Budgets_UserId_StartDate ON Budgets(UserId, StartDate DESC)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_FixedCharges_UserId_IsActive ON FixedCharges(UserId, IsActive)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_AlertThresholds_UserId_IsActive ON AlertThresholds(UserId, IsActive)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Categories_UserId_IsSystem_IsActive ON Categories(UserId, IsSystem, IsActive)");
        }

        private static bool HasColumn(SQLiteConnection connection, string tableName, string columnName)
        {
            List<SQLiteConnection.ColumnInfo> columns = connection.GetTableInfo(tableName);

            return columns.Any(column =>
                string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));
        }

        private static void SeedDefaultCategories(SQLiteConnection connection)
        {
            List<Category> defaultCategories =
            [
                new() { Name = "Alimentation", Color = "#4CAF50", Icon = "", DisplayOrder = 1, IsSystem = true, UserId = null },
                new() { Name = "Transport", Color = "#2196F3", Icon = "", DisplayOrder = 2, IsSystem = true, UserId = null },
                new() { Name = "Logement", Color = "#FF9800", Icon = "", DisplayOrder = 3, IsSystem = true, UserId = null },
                new() { Name = "Santé", Color = "#F44336", Icon = "", DisplayOrder = 4, IsSystem = true, UserId = null },
                new() { Name = "Loisirs", Color = "#9C27B0", Icon = "", DisplayOrder = 5, IsSystem = true, UserId = null },
                new() { Name = "Vêtements", Color = "#E91E63", Icon = "", DisplayOrder = 6, IsSystem = true, UserId = null },
                new() { Name = "Éducation", Color = "#3F51B5", Icon = "", DisplayOrder = 7, IsSystem = true, UserId = null },
                new() { Name = "Autres", Color = "#607D8B", Icon = "", DisplayOrder = 8, IsSystem = true, UserId = null }
            ];

            foreach (Category category in defaultCategories)
            {
                bool exists = connection.Table<Category>()
                    .Any(c => c.Name == category.Name && c.IsSystem);

                if (!exists)
                    connection.Insert(category);
            }
        }

        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();

        private static bool IsValidUserId(int userId)
            => userId > 0;

        private static List<T> OrderByInMemory<T, TKey1, TKey2>(
            IEnumerable<T> source,
            Func<T, TKey1> primaryOrder,
            Func<T, TKey2> secondaryOrder,
            bool primaryDescending = false,
            bool secondaryDescending = false)
        {
            IOrderedEnumerable<T> ordered = primaryDescending
                ? source.OrderByDescending(primaryOrder)
                : source.OrderBy(primaryOrder);

            ordered = secondaryDescending
                ? ordered.ThenByDescending(secondaryOrder)
                : ordered.ThenBy(secondaryOrder);

            return ordered.ToList();
        }

        private bool CategoryExistsForUser(SQLiteConnection connection, int categoryId, int userId)
        {
            if (categoryId <= 0 || !IsValidUserId(userId))
                return false;

            return connection.Table<Category>()
                .Any(c => c.Id == categoryId &&
                          c.IsActive &&
                          (c.IsSystem || c.UserId == userId));
        }

        private bool BudgetExistsForUser(SQLiteConnection connection, int budgetId, int userId)
        {
            if (budgetId <= 0 || !IsValidUserId(userId))
                return false;

            return connection.Table<Budget>()
                .Any(b => b.Id == budgetId && b.UserId == userId);
        }

        #region Users

        public List<User> GetUsers()
        {
            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<User>()
                    .ToList()
                    .OrderByDescending(u => u.CreatedAt)
                    .ThenBy(u => u.Email)
                    .ToList();
            }
        }

        public User? GetUserById(int id)
        {
            if (id <= 0)
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                return connection.Table<User>().FirstOrDefault(u => u.Id == id);
            }
        }

        public User? GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            string normalizedEmail = NormalizeEmail(email);

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                return connection.Table<User>().FirstOrDefault(u => u.Email == normalizedEmail);
            }
        }

        public int InsertUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(user.Email))
                return 0;

            user.Email = NormalizeEmail(user.Email);

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                connection.Insert(user);
                return user.Id;
            }
        }

        public int UpdateUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.Id <= 0 || string.IsNullOrWhiteSpace(user.Email))
                return 0;

            user.Email = NormalizeEmail(user.Email);

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                User? existingUser = connection.Table<User>()
                    .FirstOrDefault(u => u.Id == user.Id);

                if (existingUser is null)
                    return 0;

                return connection.Update(user);
            }
        }

        public int DeleteUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.Id <= 0)
                return 0;

            User? existingUser = GetUserById(user.Id);
            if (existingUser is null)
                return 0;

            DeleteAllUserData(user.Id);
            return 1;
        }

        #endregion

        #region Categories

        public List<Category> GetCategories()
        {
            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Category>()
                    .Where(c => c.IsActive && c.IsSystem)
                    .ToList()
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToList();
            }
        }

        public List<Category> GetCustomCategoriesByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return [];

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Category>()
                    .Where(c => c.IsActive && !c.IsSystem && c.UserId == userId)
                    .ToList()
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToList();
            }
        }

        public Category? GetCategoryById(int id)
        {
            if (id <= 0)
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                return connection.Table<Category>().FirstOrDefault(c => c.Id == id);
            }
        }

        public Category? GetCategoryById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Category>()
                    .FirstOrDefault(c => c.Id == id && (c.IsSystem || c.UserId == userId));
            }
        }
        public List<Category> GetCategoriesByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return [];

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Category>()
                    .Where(c => c.IsActive && (c.IsSystem || c.UserId == userId))
                    .ToList()
                    .OrderByDescending(c => c.IsSystem)
                    .ThenBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToList();
            }
        }
        public int InsertCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.IsSystem)
                category.UserId = null;
            else if (!category.UserId.HasValue || !IsValidUserId(category.UserId.Value))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                connection.Insert(category);
                return category.Id;
            }
        }

        public int UpdateCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.Id <= 0 || !category.UserId.HasValue || !IsValidUserId(category.UserId.Value))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                Category? existingCategory = connection.Table<Category>()
                    .FirstOrDefault(c => c.Id == category.Id &&
                                         !c.IsSystem &&
                                         c.UserId == category.UserId);

                if (existingCategory is null)
                    return 0;

                category.IsSystem = false;
                return connection.Update(category);
            }
        }

        public int DeleteCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.Id <= 0 || !category.UserId.HasValue || !IsValidUserId(category.UserId.Value))
                return 0;

            int deletedCategoryRows = 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                Category? existingCategory = connection.Table<Category>()
                    .FirstOrDefault(c => c.Id == category.Id &&
                                         !c.IsSystem &&
                                         c.UserId == category.UserId);

                if (existingCategory is null || !existingCategory.UserId.HasValue)
                    return 0;

                int userId = existingCategory.UserId.Value;

                connection.RunInTransaction(() =>
                {
                    connection.Execute("DELETE FROM AlertThresholds WHERE UserId = ? AND CategoryId = ?", userId, existingCategory.Id);
                    connection.Execute("DELETE FROM Expenses WHERE UserId = ? AND CategoryId = ?", userId, existingCategory.Id);
                    connection.Execute("DELETE FROM FixedCharges WHERE UserId = ? AND CategoryId = ?", userId, existingCategory.Id);

                    deletedCategoryRows = connection.Execute(
                        "DELETE FROM Categories WHERE Id = ? AND UserId = ? AND IsSystem = 0",
                        existingCategory.Id,
                        userId);
                });
            }

            return deletedCategoryRows;
        }

        #endregion

        #region Expenses

        public List<Expense> GetExpensesByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return [];

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Expense>()
                    .Where(e => e.UserId == userId)
                    .ToList()
                    .OrderByDescending(e => e.DateOperation)
                    .ThenByDescending(e => e.Id)
                    .ToList();
            }
        }

        public List<Expense> GetExpensesByCategory(int userId, int categoryId)
        {
            if (!IsValidUserId(userId) || categoryId <= 0)
                return [];

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Expense>()
                    .Where(e => e.UserId == userId && e.CategoryId == categoryId)
                    .ToList()
                    .OrderByDescending(e => e.DateOperation)
                    .ThenByDescending(e => e.Id)
                    .ToList();
            }
        }

        public Expense? GetExpenseById(int id)
        {
            if (id <= 0)
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                return connection.Table<Expense>().FirstOrDefault(e => e.Id == id);
            }
        }

        public Expense? GetExpenseById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Expense>()
                    .FirstOrDefault(e => e.Id == id && e.UserId == userId);
            }
        }

        public int InsertExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (!IsValidUserId(expense.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                if (!CategoryExistsForUser(connection, expense.CategoryId, expense.UserId))
                    return 0;

                connection.Insert(expense);
                return expense.Id;
            }
        }

        public int UpdateExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (!IsValidUserId(expense.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                if (!CategoryExistsForUser(connection, expense.CategoryId, expense.UserId))
                    return 0;

                Expense? existingExpense = connection.Table<Expense>()
                    .FirstOrDefault(e => e.Id == expense.Id && e.UserId == expense.UserId);

                if (existingExpense is null)
                    return 0;

                return connection.Update(expense);
            }
        }

        public int DeleteExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (!IsValidUserId(expense.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                Expense? existingExpense = connection.Table<Expense>()
                    .FirstOrDefault(e => e.Id == expense.Id && e.UserId == expense.UserId);

                if (existingExpense is null)
                    return 0;

                return connection.Delete(existingExpense);
            }
        }

        #endregion

        #region Budgets

        public List<Budget> GetBudgetsByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return [];

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Budget>()
                    .Where(b => b.UserId == userId && b.IsActive)
                    .ToList()
                    .OrderByDescending(b => b.StartDate)
                    .ThenByDescending(b => b.CreatedAt)
                    .ToList();
            }
        }

        public Budget? GetBudgetById(int id)
        {
            if (id <= 0)
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                return connection.Table<Budget>().FirstOrDefault(b => b.Id == id);
            }
        }

        public Budget? GetBudgetById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<Budget>()
                    .FirstOrDefault(b => b.Id == id && b.UserId == userId);
            }
        }

        public int InsertBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (!IsValidUserId(budget.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                connection.Insert(budget);
                return budget.Id;
            }
        }

        public int UpdateBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (!IsValidUserId(budget.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                Budget? existingBudget = connection.Table<Budget>()
                    .FirstOrDefault(b => b.Id == budget.Id && b.UserId == budget.UserId);

                if (existingBudget is null)
                    return 0;

                return connection.Update(budget);
            }
        }

        public int DeleteBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (!IsValidUserId(budget.UserId))
                return 0;

            int deletedBudgetRows = 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                Budget? existingBudget = connection.Table<Budget>()
                    .FirstOrDefault(b => b.Id == budget.Id && b.UserId == budget.UserId);

                if (existingBudget is null)
                    return 0;

                connection.RunInTransaction(() =>
                {
                    connection.Execute("DELETE FROM AlertThresholds WHERE UserId = ? AND BudgetId = ?", budget.UserId, budget.Id);
                    deletedBudgetRows = connection.Delete(existingBudget);
                });
            }

            return deletedBudgetRows;
        }

        #endregion

        #region FixedCharges

        public List<FixedCharge> GetFixedChargesByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return [];

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<FixedCharge>()
                    .Where(f => f.UserId == userId && f.IsActive)
                    .ToList()
                    .OrderBy(f => f.DayOfMonth)
                    .ThenBy(f => f.Name)
                    .ToList();
            }
        }

        public FixedCharge? GetFixedChargeById(int id)
        {
            if (id <= 0)
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                return connection.Table<FixedCharge>().FirstOrDefault(f => f.Id == id);
            }
        }

        public FixedCharge? GetFixedChargeById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<FixedCharge>()
                    .FirstOrDefault(f => f.Id == id && f.UserId == userId);
            }
        }

        public int InsertFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (!IsValidUserId(fixedCharge.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                if (!CategoryExistsForUser(connection, fixedCharge.CategoryId, fixedCharge.UserId))
                    return 0;

                connection.Insert(fixedCharge);
                return fixedCharge.Id;
            }
        }

        public int UpdateFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (!IsValidUserId(fixedCharge.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                if (!CategoryExistsForUser(connection, fixedCharge.CategoryId, fixedCharge.UserId))
                    return 0;

                FixedCharge? existingFixedCharge = connection.Table<FixedCharge>()
                    .FirstOrDefault(f => f.Id == fixedCharge.Id && f.UserId == fixedCharge.UserId);

                if (existingFixedCharge is null)
                    return 0;

                return connection.Update(fixedCharge);
            }
        }

        public int DeleteFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (!IsValidUserId(fixedCharge.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                FixedCharge? existingFixedCharge = connection.Table<FixedCharge>()
                    .FirstOrDefault(f => f.Id == fixedCharge.Id && f.UserId == fixedCharge.UserId);

                if (existingFixedCharge is null)
                    return 0;

                return connection.Delete(existingFixedCharge);
            }
        }

        public int GetActiveFixedChargesCountByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<FixedCharge>()
                    .Count(f => f.UserId == userId && f.IsActive);
            }
        }

        #endregion

        #region AlertThresholds

        public List<AlertThreshold> GetAlertThresholdsByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return [];

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<AlertThreshold>()
                    .Where(a => a.UserId == userId && a.IsActive)
                    .ToList()
                    .OrderByDescending(a => a.ThresholdPercentage)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToList();
            }
        }

        public AlertThreshold? GetAlertThresholdById(int id)
        {
            if (id <= 0)
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();
                return connection.Table<AlertThreshold>().FirstOrDefault(a => a.Id == id);
            }
        }

        public AlertThreshold? GetAlertThresholdById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                return connection.Table<AlertThreshold>()
                    .FirstOrDefault(a => a.Id == id && a.UserId == userId);
            }
        }

        public int InsertAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (!IsValidUserId(alertThreshold.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                if (alertThreshold.BudgetId.HasValue &&
                    !BudgetExistsForUser(connection, alertThreshold.BudgetId.Value, alertThreshold.UserId))
                    return 0;

                if (alertThreshold.CategoryId.HasValue &&
                    !CategoryExistsForUser(connection, alertThreshold.CategoryId.Value, alertThreshold.UserId))
                    return 0;

                connection.Insert(alertThreshold);
                return alertThreshold.Id;
            }
        }

        public int UpdateAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (!IsValidUserId(alertThreshold.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                if (alertThreshold.BudgetId.HasValue &&
                    !BudgetExistsForUser(connection, alertThreshold.BudgetId.Value, alertThreshold.UserId))
                    return 0;

                if (alertThreshold.CategoryId.HasValue &&
                    !CategoryExistsForUser(connection, alertThreshold.CategoryId.Value, alertThreshold.UserId))
                    return 0;

                AlertThreshold? existingAlertThreshold = connection.Table<AlertThreshold>()
                    .FirstOrDefault(a => a.Id == alertThreshold.Id && a.UserId == alertThreshold.UserId);

                if (existingAlertThreshold is null)
                    return 0;

                return connection.Update(alertThreshold);
            }
        }

        public int DeleteAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (!IsValidUserId(alertThreshold.UserId))
                return 0;

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                AlertThreshold? existingAlertThreshold = connection.Table<AlertThreshold>()
                    .FirstOrDefault(a => a.Id == alertThreshold.Id && a.UserId == alertThreshold.UserId);

                if (existingAlertThreshold is null)
                    return 0;

                return connection.Delete(existingAlertThreshold);
            }
        }

        #endregion

        public void DeleteAllUserData(int userId)
        {
            if (!IsValidUserId(userId))
                throw new ArgumentOutOfRangeException(nameof(userId));

            lock (_dbLock)
            {
                SQLiteConnection connection = GetOrCreateConnection();

                connection.RunInTransaction(() =>
                {
                    connection.Execute("DELETE FROM AlertThresholds WHERE UserId = ?", userId);
                    connection.Execute("DELETE FROM Expenses WHERE UserId = ?", userId);
                    connection.Execute("DELETE FROM FixedCharges WHERE UserId = ?", userId);
                    connection.Execute("DELETE FROM Budgets WHERE UserId = ?", userId);
                    connection.Execute("DELETE FROM Categories WHERE UserId = ? AND IsSystem = 0", userId);
                    connection.Execute("DELETE FROM Users WHERE Id = ?", userId);
                });
            }
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
    }
}
