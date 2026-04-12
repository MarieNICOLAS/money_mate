using MoneyMate.Models;
using SQLite;

namespace MoneyMate.Data.Context
{
    /// <summary>
    /// Contexte de base de données SQLite pour Money Mate.
    /// Centralise les opérations CRUD synchrones et l'initialisation de la base.
    /// </summary>
    public sealed class MoneyMateDbContext : IMoneyMateDbContext, IDisposable
    {
        private readonly string _dbPath;
        private SQLiteConnection? _connection;

        /// <summary>
        /// Initialise un nouveau contexte SQLite.
        /// </summary>
        /// <param name="dbPath">Chemin complet du fichier SQLite.</param>
        /// <exception cref="ArgumentException">Levée si le chemin est vide.</exception>
        public MoneyMateDbContext(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("Le chemin de la base de données est requis.", nameof(dbPath));

            _dbPath = dbPath;

            var directoryPath = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }

        /// <summary>
        /// Obtient une connexion SQLite initialisée.
        /// </summary>
        private SQLiteConnection Database
        {
            get
            {
                if (_connection != null)
                    return _connection;

                try
                {
                    _connection = new SQLiteConnection(_dbPath);
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
        }

        /// <summary>
        /// Crée les tables et injecte les données par défaut.
        /// </summary>
        /// <param name="connection">Connexion SQLite active.</param>
        private static void InitializeDatabase(SQLiteConnection connection)
        {
            connection.CreateTable<User>();
            connection.CreateTable<Category>();
            connection.CreateTable<Budget>();
            connection.CreateTable<Expense>();
            connection.CreateTable<FixedCharge>();
            connection.CreateTable<AlertThreshold>();

            EnsureSchemaUpToDate(connection);
            SeedDefaultCategories(connection);
        }

        /// <summary>
        /// Applique les évolutions de schéma nécessaires.
        /// </summary>
        /// <param name="connection">Connexion SQLite active.</param>
        private static void EnsureSchemaUpToDate(SQLiteConnection connection)
        {
            EnsureCategoriesSchema(connection);
        }

        /// <summary>
        /// Ajoute les colonnes manquantes sur la table des catégories.
        /// </summary>
        /// <param name="connection">Connexion SQLite active.</param>
        private static void EnsureCategoriesSchema(SQLiteConnection connection)
        {
            if (!HasColumn(connection, "Categories", "UserId"))
                connection.Execute("ALTER TABLE Categories ADD COLUMN UserId INTEGER NULL");

            if (!HasColumn(connection, "Categories", "IsSystem"))
                connection.Execute("ALTER TABLE Categories ADD COLUMN IsSystem INTEGER NOT NULL DEFAULT 0");

            connection.Execute("UPDATE Categories SET IsSystem = 1 WHERE UserId IS NULL");
        }

        /// <summary>
        /// Indique si une colonne existe sur une table.
        /// </summary>
        /// <param name="connection">Connexion SQLite active.</param>
        /// <param name="tableName">Nom de la table.</param>
        /// <param name="columnName">Nom de la colonne.</param>
        private static bool HasColumn(SQLiteConnection connection, string tableName, string columnName)
        {
            var columns = connection.GetTableInfo(tableName);

            return columns.Any(column =>
                string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Insère les catégories par défaut au premier lancement.
        /// </summary>
        /// <param name="connection">Connexion SQLite active.</param>
        private static void SeedDefaultCategories(SQLiteConnection connection)
        {
            var defaultCategories = new List<Category>
            {
                new() { Name = "Alimentation", Color = "#4CAF50", Icon = "", DisplayOrder = 1, IsSystem = true, UserId = null },
                new() { Name = "Transport", Color = "#2196F3", Icon = "", DisplayOrder = 2, IsSystem = true, UserId = null },
                new() { Name = "Logement", Color = "#FF9800", Icon = "", DisplayOrder = 3, IsSystem = true, UserId = null },
                new() { Name = "Santé", Color = "#F44336", Icon = "", DisplayOrder = 4, IsSystem = true, UserId = null },
                new() { Name = "Loisirs", Color = "#9C27B0", Icon = "", DisplayOrder = 5, IsSystem = true, UserId = null },
                new() { Name = "Vêtements", Color = "#E91E63", Icon = "", DisplayOrder = 6, IsSystem = true, UserId = null },
                new() { Name = "Éducation", Color = "#3F51B5", Icon = "", DisplayOrder = 7, IsSystem = true, UserId = null },
                new() { Name = "Autres", Color = "#607D8B", Icon = "", DisplayOrder = 8, IsSystem = true, UserId = null }
            };

            foreach (var category in defaultCategories)
            {
                bool exists = connection.Table<Category>().Any(c =>
                    c.Name == category.Name &&
                    c.IsSystem);

                if (!exists)
                    connection.Insert(category);
            }
        }

        /// <summary>
        /// Normalise un email pour garantir une comparaison cohérente.
        /// </summary>
        /// <param name="email">Email brut.</param>
        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();

        /// <summary>
        /// Indique si un identifiant utilisateur est valide.
        /// </summary>
        /// <param name="userId">Identifiant utilisateur.</param>
        private static bool IsValidUserId(int userId)
            => userId > 0;

        /// <summary>
        /// Vérifie qu'une catégorie est accessible à un utilisateur.
        /// </summary>
        /// <param name="categoryId">Identifiant de la catégorie.</param>
        /// <param name="userId">Identifiant utilisateur.</param>
        private bool CategoryExistsForUser(int categoryId, int userId)
        {
            if (categoryId <= 0 || !IsValidUserId(userId))
                return false;

            return Database.Table<Category>()
                           .Any(c => c.Id == categoryId &&
                                     c.IsActive &&
                                     (c.IsSystem || c.UserId == userId));
        }

        /// <summary>
        /// Vérifie qu'un budget appartient à un utilisateur.
        /// </summary>
        /// <param name="budgetId">Identifiant du budget.</param>
        /// <param name="userId">Identifiant utilisateur.</param>
        private bool BudgetExistsForUser(int budgetId, int userId)
        {
            if (budgetId <= 0 || !IsValidUserId(userId))
                return false;

            return Database.Table<Budget>()
                           .Any(b => b.Id == budgetId && b.UserId == userId);
        }

        #region Users
        /// <summary>
        /// Retourne tous les utilisateurs.
        /// </summary>
        public List<User> GetUsers()
            => Database.Table<User>()
                       .ToList()
                       .OrderByDescending(u => u.CreatedAt)
                       .ThenBy(u => u.Email)
                       .ToList();

        /// <summary>
        /// Retourne un utilisateur par son identifiant.
        /// </summary>
        public User? GetUserById(int id)
        {
            if (id <= 0)
                return null;

            return Database.Table<User>().FirstOrDefault(u => u.Id == id);
        }

        /// <summary>
        /// Retourne un utilisateur par son email normalisé.
        /// </summary>
        public User? GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            string normalizedEmail = NormalizeEmail(email);

            return Database.Table<User>()
                           .ToList()
                           .FirstOrDefault(u =>
                               !string.IsNullOrWhiteSpace(u.Email) &&
                               string.Equals(
                                   u.Email.Trim(),
                                   normalizedEmail,
                                   StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Insère un utilisateur et retourne son identifiant.
        /// </summary>
        public int InsertUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(user.Email))
                return 0;

            user.Email = NormalizeEmail(user.Email);

            Database.Insert(user);
            return user.Id;
        }

        /// <summary>
        /// Met à jour un utilisateur.
        /// </summary>
        public int UpdateUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.Id <= 0 || string.IsNullOrWhiteSpace(user.Email))
                return 0;

            var existingUser = GetUserById(user.Id);
            if (existingUser == null)
                return 0;

            user.Email = NormalizeEmail(user.Email);

            return Database.Update(user);
        }

        /// <summary>
        /// Supprime un utilisateur et toutes ses données liées.
        /// </summary>
        public int DeleteUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (user.Id <= 0)
                return 0;

            var existingUser = GetUserById(user.Id);
            if (existingUser == null)
                return 0;

            DeleteAllUserData(user.Id);
            return 1;
        }
        #endregion

        #region Categories
        /// <summary>
        /// Retourne uniquement les catégories système actives.
        /// </summary>
        public List<Category> GetCategories()
            => Database.Table<Category>()
                       .Where(c => c.IsActive && c.IsSystem)
                       .ToList()
                       .OrderBy(c => c.DisplayOrder)
                       .ThenBy(c => c.Name)
                       .ToList();

        /// <summary>
        /// Retourne les catégories accessibles à un utilisateur :
        /// catégories système + catégories personnalisées de cet utilisateur.
        /// </summary>
        public List<Category> GetCategoriesByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return new List<Category>();

            return Database.Table<Category>()
                           .Where(c => c.IsActive && (c.IsSystem || c.UserId == userId))
                           .ToList()
                           .OrderByDescending(c => c.IsSystem)
                           .ThenBy(c => c.DisplayOrder)
                           .ThenBy(c => c.Name)
                           .ToList();
        }

        /// <summary>
        /// Retourne uniquement les catégories personnalisées d'un utilisateur.
        /// </summary>
        public List<Category> GetCustomCategoriesByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return new List<Category>();

            return Database.Table<Category>()
                           .Where(c => c.IsActive && !c.IsSystem && c.UserId == userId)
                           .ToList()
                           .OrderBy(c => c.DisplayOrder)
                           .ThenBy(c => c.Name)
                           .ToList();
        }

        /// <summary>
        /// Retourne une catégorie par son identifiant.
        /// </summary>
        public Category? GetCategoryById(int id)
        {
            if (id <= 0)
                return null;

            return Database.Table<Category>().FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Retourne une catégorie accessible à un utilisateur par son identifiant.
        /// </summary>
        public Category? GetCategoryById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            return Database.Table<Category>()
                           .FirstOrDefault(c => c.Id == id && (c.IsSystem || c.UserId == userId));
        }

        /// <summary>
        /// Insère une catégorie et retourne son identifiant.
        /// </summary>
        public int InsertCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.IsSystem)
                category.UserId = null;
            else if (!category.UserId.HasValue || !IsValidUserId(category.UserId.Value))
                return 0;

            Database.Insert(category);
            return category.Id;
        }

        /// <summary>
        /// Met à jour une catégorie personnalisée.
        /// Les catégories système ne peuvent pas être modifiées ici.
        /// </summary>
        public int UpdateCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.Id <= 0 || !category.UserId.HasValue || !IsValidUserId(category.UserId.Value))
                return 0;

            var existingCategory = Database.Table<Category>()
                                           .FirstOrDefault(c =>
                                               c.Id == category.Id &&
                                               !c.IsSystem &&
                                               c.UserId == category.UserId);

            if (existingCategory == null)
                return 0;

            category.IsSystem = false;

            return Database.Update(category);
        }

        /// <summary>
        /// Supprime une catégorie personnalisée ainsi que ses données liées.
        /// </summary>
        public int DeleteCategory(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);

            if (category.Id <= 0 || !category.UserId.HasValue || !IsValidUserId(category.UserId.Value))
                return 0;

            var existingCategory = Database.Table<Category>()
                                           .FirstOrDefault(c =>
                                               c.Id == category.Id &&
                                               !c.IsSystem &&
                                               c.UserId == category.UserId);

            if (existingCategory == null || !existingCategory.UserId.HasValue)
                return 0;

            int deletedCategoryRows = 0;
            int userId = existingCategory.UserId.Value;
            var database = Database;

            database.RunInTransaction(() =>
            {
                database.Execute(
                    "DELETE FROM AlertThresholds WHERE UserId = ? AND CategoryId = ?",
                    userId,
                    existingCategory.Id);

                database.Execute(
                    "DELETE FROM Expenses WHERE UserId = ? AND CategoryId = ?",
                    userId,
                    existingCategory.Id);

                database.Execute(
                    "DELETE FROM FixedCharges WHERE UserId = ? AND CategoryId = ?",
                    userId,
                    existingCategory.Id);

                deletedCategoryRows = database.Execute(
                    "DELETE FROM Categories WHERE Id = ? AND UserId = ? AND IsSystem = 0",
                    existingCategory.Id,
                    userId);
            });

            return deletedCategoryRows;
        }
        #endregion

        #region Expenses
        /// <summary>
        /// Retourne les dépenses d'un utilisateur, triées de la plus récente à la plus ancienne.
        /// </summary>
        public List<Expense> GetExpensesByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return new List<Expense>();

            return Database.Table<Expense>()
                           .Where(e => e.UserId == userId)
                           .ToList()
                           .OrderByDescending(e => e.DateOperation)
                           .ThenByDescending(e => e.Id)
                           .ToList();
        }

        /// <summary>
        /// Retourne les dépenses d'un utilisateur pour une catégorie donnée.
        /// </summary>
        public List<Expense> GetExpensesByCategory(int userId, int categoryId)
        {
            if (!IsValidUserId(userId) || categoryId <= 0)
                return new List<Expense>();

            return Database.Table<Expense>()
                           .Where(e => e.UserId == userId && e.CategoryId == categoryId)
                           .ToList()
                           .OrderByDescending(e => e.DateOperation)
                           .ThenByDescending(e => e.Id)
                           .ToList();
        }

        /// <summary>
        /// Retourne une dépense par son identifiant.
        /// </summary>
        public Expense? GetExpenseById(int id)
        {
            if (id <= 0)
                return null;

            return Database.Table<Expense>().FirstOrDefault(e => e.Id == id);
        }

        /// <summary>
        /// Retourne une dépense par son identifiant en la restreignant à un utilisateur.
        /// </summary>
        public Expense? GetExpenseById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            return Database.Table<Expense>()
                           .FirstOrDefault(e => e.Id == id && e.UserId == userId);
        }

        /// <summary>
        /// Insère une dépense et retourne son identifiant.
        /// </summary>
        public int InsertExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (!IsValidUserId(expense.UserId))
                return 0;

            if (!CategoryExistsForUser(expense.CategoryId, expense.UserId))
                return 0;

            Database.Insert(expense);
            return expense.Id;
        }

        /// <summary>
        /// Met à jour une dépense.
        /// </summary>
        public int UpdateExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (!IsValidUserId(expense.UserId))
                return 0;

            var existingExpense = GetExpenseById(expense.Id, expense.UserId);
            if (existingExpense == null)
                return 0;

            if (!CategoryExistsForUser(expense.CategoryId, expense.UserId))
                return 0;

            return Database.Update(expense);
        }

        /// <summary>
        /// Supprime une dépense.
        /// </summary>
        public int DeleteExpense(Expense expense)
        {
            ArgumentNullException.ThrowIfNull(expense);

            if (!IsValidUserId(expense.UserId))
                return 0;

            var existingExpense = GetExpenseById(expense.Id, expense.UserId);
            if (existingExpense == null)
                return 0;

            return Database.Delete(existingExpense);
        }
        #endregion

        #region Budgets
        /// <summary>
        /// Retourne les budgets actifs d'un utilisateur.
        /// </summary>
        public List<Budget> GetBudgetsByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return new List<Budget>();

            return Database.Table<Budget>()
                           .Where(b => b.UserId == userId && b.IsActive)
                           .ToList()
                           .OrderByDescending(b => b.StartDate)
                           .ThenByDescending(b => b.CreatedAt)
                           .ToList();
        }

        /// <summary>
        /// Retourne un budget par son identifiant.
        /// </summary>
        public Budget? GetBudgetById(int id)
        {
            if (id <= 0)
                return null;

            return Database.Table<Budget>().FirstOrDefault(b => b.Id == id);
        }

        /// <summary>
        /// Retourne un budget par son identifiant en la restreignant à un utilisateur.
        /// </summary>
        public Budget? GetBudgetById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            return Database.Table<Budget>()
                           .FirstOrDefault(b => b.Id == id && b.UserId == userId);
        }

        /// <summary>
        /// Insère un budget et retourne son identifiant.
        /// </summary>
        public int InsertBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (!IsValidUserId(budget.UserId))
                return 0;

            Database.Insert(budget);
            return budget.Id;
        }

        /// <summary>
        /// Met à jour un budget.
        /// </summary>
        public int UpdateBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (!IsValidUserId(budget.UserId))
                return 0;

            var existingBudget = GetBudgetById(budget.Id, budget.UserId);
            if (existingBudget == null)
                return 0;

            return Database.Update(budget);
        }

        /// <summary>
        /// Supprime un budget et les seuils d'alerte qui lui sont liés.
        /// </summary>
        public int DeleteBudget(Budget budget)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (!IsValidUserId(budget.UserId))
                return 0;

            var existingBudget = GetBudgetById(budget.Id, budget.UserId);
            if (existingBudget == null)
                return 0;

            int deletedBudgetRows = 0;
            var database = Database;

            database.RunInTransaction(() =>
            {
                database.Execute(
                    "DELETE FROM AlertThresholds WHERE UserId = ? AND BudgetId = ?",
                    budget.UserId,
                    budget.Id);

                deletedBudgetRows = database.Delete(existingBudget);
            });

            return deletedBudgetRows;
        }
        #endregion

        #region FixedCharges
        /// <summary>
        /// Retourne les charges fixes actives d'un utilisateur.
        /// </summary>
        public List<FixedCharge> GetFixedChargesByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return new List<FixedCharge>();

            return Database.Table<FixedCharge>()
                           .Where(f => f.UserId == userId && f.IsActive)
                           .ToList()
                           .OrderBy(f => f.DayOfMonth)
                           .ThenBy(f => f.Name)
                           .ToList();
        }

        /// <summary>
        /// Retourne une charge fixe par son identifiant.
        /// </summary>
        public FixedCharge? GetFixedChargeById(int id)
        {
            if (id <= 0)
                return null;

            return Database.Table<FixedCharge>().FirstOrDefault(f => f.Id == id);
        }

        /// <summary>
        /// Retourne une charge fixe par son identifiant en la restreignant à un utilisateur.
        /// </summary>
        public FixedCharge? GetFixedChargeById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            return Database.Table<FixedCharge>()
                           .FirstOrDefault(f => f.Id == id && f.UserId == userId);
        }

        /// <summary>
        /// Insère une charge fixe et retourne son identifiant.
        /// </summary>
        public int InsertFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (!IsValidUserId(fixedCharge.UserId))
                return 0;

            if (!CategoryExistsForUser(fixedCharge.CategoryId, fixedCharge.UserId))
                return 0;

            Database.Insert(fixedCharge);
            return fixedCharge.Id;
        }

        /// <summary>
        /// Met à jour une charge fixe.
        /// </summary>
        public int UpdateFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (!IsValidUserId(fixedCharge.UserId))
                return 0;

            var existingFixedCharge = GetFixedChargeById(fixedCharge.Id, fixedCharge.UserId);
            if (existingFixedCharge == null)
                return 0;

            if (!CategoryExistsForUser(fixedCharge.CategoryId, fixedCharge.UserId))
                return 0;

            return Database.Update(fixedCharge);
        }

        /// <summary>
        /// Supprime une charge fixe.
        /// </summary>
        public int DeleteFixedCharge(FixedCharge fixedCharge)
        {
            ArgumentNullException.ThrowIfNull(fixedCharge);

            if (!IsValidUserId(fixedCharge.UserId))
                return 0;

            var existingFixedCharge = GetFixedChargeById(fixedCharge.Id, fixedCharge.UserId);
            if (existingFixedCharge == null)
                return 0;

            return Database.Delete(existingFixedCharge);
        }
        #endregion

        #region AlertThresholds
        /// <summary>
        /// Retourne les seuils d'alerte actifs d'un utilisateur.
        /// </summary>
        public List<AlertThreshold> GetAlertThresholdsByUserId(int userId)
        {
            if (!IsValidUserId(userId))
                return new List<AlertThreshold>();

            return Database.Table<AlertThreshold>()
                           .Where(a => a.UserId == userId && a.IsActive)
                           .ToList()
                           .OrderByDescending(a => a.ThresholdPercentage)
                           .ThenByDescending(a => a.CreatedAt)
                           .ToList();
        }

        /// <summary>
        /// Retourne un seuil d'alerte par son identifiant.
        /// </summary>
        public AlertThreshold? GetAlertThresholdById(int id)
        {
            if (id <= 0)
                return null;

            return Database.Table<AlertThreshold>().FirstOrDefault(a => a.Id == id);
        }

        /// <summary>
        /// Retourne un seuil d'alerte par son identifiant en la restreignant à un utilisateur.
        /// </summary>
        public AlertThreshold? GetAlertThresholdById(int id, int userId)
        {
            if (id <= 0 || !IsValidUserId(userId))
                return null;

            return Database.Table<AlertThreshold>()
                           .FirstOrDefault(a => a.Id == id && a.UserId == userId);
        }

        /// <summary>
        /// Insère un seuil d'alerte et retourne son identifiant.
        /// </summary>
        public int InsertAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (!IsValidUserId(alertThreshold.UserId))
                return 0;

            if (alertThreshold.BudgetId.HasValue &&
                !BudgetExistsForUser(alertThreshold.BudgetId.Value, alertThreshold.UserId))
                return 0;

            if (alertThreshold.CategoryId.HasValue &&
                !CategoryExistsForUser(alertThreshold.CategoryId.Value, alertThreshold.UserId))
                return 0;

            Database.Insert(alertThreshold);
            return alertThreshold.Id;
        }

        /// <summary>
        /// Met à jour un seuil d'alerte.
        /// </summary>
        public int UpdateAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (!IsValidUserId(alertThreshold.UserId))
                return 0;

            var existingAlertThreshold = GetAlertThresholdById(alertThreshold.Id, alertThreshold.UserId);
            if (existingAlertThreshold == null)
                return 0;

            if (alertThreshold.BudgetId.HasValue &&
                !BudgetExistsForUser(alertThreshold.BudgetId.Value, alertThreshold.UserId))
                return 0;

            if (alertThreshold.CategoryId.HasValue &&
                !CategoryExistsForUser(alertThreshold.CategoryId.Value, alertThreshold.UserId))
                return 0;

            return Database.Update(alertThreshold);
        }

        /// <summary>
        /// Supprime un seuil d'alerte.
        /// </summary>
        public int DeleteAlertThreshold(AlertThreshold alertThreshold)
        {
            ArgumentNullException.ThrowIfNull(alertThreshold);

            if (!IsValidUserId(alertThreshold.UserId))
                return 0;

            var existingAlertThreshold = GetAlertThresholdById(alertThreshold.Id, alertThreshold.UserId);
            if (existingAlertThreshold == null)
                return 0;

            return Database.Delete(existingAlertThreshold);
        }
        #endregion

        /// <summary>
        /// Supprime toutes les données liées à un utilisateur.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur à supprimer.</param>
        public void DeleteAllUserData(int userId)
        {
            if (!IsValidUserId(userId))
                throw new ArgumentOutOfRangeException(nameof(userId));

            var database = Database;

            database.RunInTransaction(() =>
            {
                database.Execute("DELETE FROM AlertThresholds WHERE UserId = ?", userId);
                database.Execute("DELETE FROM Expenses WHERE UserId = ?", userId);
                database.Execute("DELETE FROM FixedCharges WHERE UserId = ?", userId);
                database.Execute("DELETE FROM Budgets WHERE UserId = ?", userId);
                database.Execute("DELETE FROM Categories WHERE UserId = ? AND IsSystem = 0", userId);
                database.Execute("DELETE FROM Users WHERE Id = ?", userId);
            });
        }

        /// <summary>
        /// Ferme explicitement la connexion SQLite.
        /// </summary>
        public void Close()
        {
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }

        /// <summary>
        /// Libère les ressources associées au contexte.
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
