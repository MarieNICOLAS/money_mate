using MoneyMate.Models;
using SQLite;

namespace MoneyMate.Data.Context
{
    public sealed class MoneyMateDbContext : IMoneyMateDbContext, IDisposable
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

        public SQLiteConnection GetConnectionSafe()
        {
            lock (_dbLock)
            {
                return GetOrCreateConnection();
            }
        }

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

            return ExecuteSafe(connection => connection.Insert(user));
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

            return Execute(connection => connection.Table<Category>()
                .Where(category => (category.IsSystem || category.UserId == userId) && category.IsActive)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToList());
        }

        public List<Category> GetAllCategoriesByUserId(int userId)
        {
            if (userId <= 0)
                return [];

            return Execute(connection => connection.Table<Category>()
                .Where(category => category.IsSystem || category.UserId == userId)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .ToList());
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

        public int InsertCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (!category.IsSystem && category.UserId <= 0)
                return 0;

            if (string.IsNullOrWhiteSpace(category.Name))
                return 0;

            category.Name = category.Name.Trim();
            category.Color = NormalizeColor(category.Color);

            return ExecuteSafe(connection => connection.Insert(category));
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

            return ExecuteSafe(connection => connection.Insert(expense));
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

            return ExecuteSafe(connection => connection.Insert(budget));
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

            return ExecuteSafe(connection => connection.Insert(fixedCharge));
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

            return ExecuteSafe(connection => connection.Insert(alertThreshold));
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
            CreateIndexes(connection);
            SeedCategories(connection);
        }

        private static void EnsureBudgetCategoryColumn(SQLiteConnection connection)
        {
            bool hasCategoryIdColumn = connection.GetTableInfo("Budgets")
                .Any(column => string.Equals(column.Name, nameof(Budget.CategoryId), StringComparison.OrdinalIgnoreCase));

            if (!hasCategoryIdColumn)
                connection.Execute("ALTER TABLE Budgets ADD COLUMN CategoryId INTEGER NOT NULL DEFAULT 0;");
        }

        private static void CreateIndexes(SQLiteConnection connection)
        {
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Users_Email ON Users(Email)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Expenses_User_Date ON Expenses(UserId, DateOperation DESC)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Expenses_Category ON Expenses(CategoryId)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Categories_User_IsSystem ON Categories(UserId, IsSystem)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_Budgets_User_StartDate ON Budgets(UserId, StartDate DESC)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_FixedCharges_User_Active ON FixedCharges(UserId, IsActive)");
            connection.Execute("CREATE INDEX IF NOT EXISTS IX_AlertThresholds_User ON AlertThresholds(UserId)");
        }

        private static void SeedCategories(SQLiteConnection connection)
        {
            if (connection.Table<Category>().Any(category => category.IsSystem))
                return;

            List<Category> categories =
            [
                new() { Name = "Alimentation", Color = "#F4B183", Icon = "🛒", DisplayOrder = 1, IsSystem = true, IsActive = true },
                new() { Name = "Loisirs", Color = "#7C92C3", Icon = "🎉", DisplayOrder = 2, IsSystem = true, IsActive = true },
                new() { Name = "Transport", Color = "#8EC9D3", Icon = "🚌", DisplayOrder = 3, IsSystem = true, IsActive = true },
                new() { Name = "Logement", Color = "#D9E2F3", Icon = "🏠", DisplayOrder = 4, IsSystem = true, IsActive = true }
            ];

            connection.InsertAll(categories);
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
            => Execute(connection => connection.Table<Category>()
                .Any(category => category.Id == categoryId
                                 && category.IsActive
                                 && (category.IsSystem || category.UserId == userId)));

        private bool IsAlertThresholdValid(AlertThreshold alertThreshold)
        {
            if (alertThreshold.UserId <= 0 || alertThreshold.ThresholdPercentage <= 0)
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
