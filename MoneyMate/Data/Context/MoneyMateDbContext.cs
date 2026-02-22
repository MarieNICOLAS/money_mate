using SQLite;
using MoneyMate.Models;

namespace MoneyMate.Data.Context
{
    /// <summary>
    /// Contexte de base de données SQLite pour Money Mate
    /// Gère toutes les opérations de base de données selon les spécifications CONTRIBUTING.md
    /// </summary>
    public class MoneyMateDbContext
    {
        private readonly string _dbPath;
        private SQLiteConnection? _connection;

        /// <summary>
        /// Constructeur du contexte de base de données
        /// </summary>
        /// <param name="dbPath">Chemin vers le fichier de base de données SQLite</param>
        public MoneyMateDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        /// <summary>
        /// Obtient une connexion à la base de données
        /// </summary>
        private SQLiteConnection Database
        {
            get
            {
                if (_connection == null)
                {
                    _connection = new SQLiteConnection(_dbPath);
                    InitializeDatabase();
                }
                return _connection;
            }
        }

        /// <summary>
        /// Initialise la base de données avec toutes les tables
        /// </summary>
        private void InitializeDatabase()
        {
            // Création des tables selon l'ordre des dépendances
            Database.CreateTable<User>();
            Database.CreateTable<Category>();
            Database.CreateTable<Budget>();
            Database.CreateTable<Expense>();
            Database.CreateTable<FixedCharge>();
            Database.CreateTable<AlertThreshold>();

            // Insertion des catégories par défaut si nécessaire
            SeedDefaultCategories();
        }

        /// <summary>
        /// Insertion des catégories par défaut
        /// </summary>
        private void SeedDefaultCategories()
        {
            if (Database.Table<Category>().Count() == 0)
            {
                var defaultCategories = new List<Category>
                {
                    new() { Name = "Alimentation", Color = "#4CAF50", Icon = "", DisplayOrder = 1 },
                    new() { Name = "Transport", Color = "#2196F3", Icon = "", DisplayOrder = 2 },
                    new() { Name = "Logement", Color = "#FF9800", Icon = "", DisplayOrder = 3 },
                    new() { Name = "Santé", Color = "#F44336", Icon = "", DisplayOrder = 4 },
                    new() { Name = "Loisirs", Color = "#9C27B0", Icon = "", DisplayOrder = 5 },
                    new() { Name = "Vêtements", Color = "#E91E63", Icon = "", DisplayOrder = 6 },
                    new() { Name = "Éducation", Color = "#3F51B5", Icon = "", DisplayOrder = 7 },
                    new() { Name = "Autres", Color = "#607D8B", Icon = "", DisplayOrder = 8 }
                };

                Database.InsertAll(defaultCategories);
            }
        }

        #region Users
        public List<User> GetUsers() => Database.Table<User>().ToList();
        public User? GetUserById(int id) => Database.Table<User>().FirstOrDefault(u => u.Id == id);
        public User? GetUserByEmail(string email) => Database.Table<User>().FirstOrDefault(u => u.Email == email);
        public int InsertUser(User user) => Database.Insert(user);
        public int UpdateUser(User user) => Database.Update(user);
        public int DeleteUser(User user) => Database.Delete(user);
        #endregion

        #region Categories
        public List<Category> GetCategories() => Database.Table<Category>().Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
        public Category? GetCategoryById(int id) => Database.Table<Category>().FirstOrDefault(c => c.Id == id);
        public int InsertCategory(Category category) => Database.Insert(category);
        public int UpdateCategory(Category category) => Database.Update(category);
        public int DeleteCategory(Category category) => Database.Delete(category);
        #endregion

        #region Expenses
        public List<Expense> GetExpensesByUserId(int userId) => Database.Table<Expense>().Where(e => e.UserId == userId).OrderByDescending(e => e.DateOperation).ToList();
        public List<Expense> GetExpensesByCategory(int userId, int categoryId) => Database.Table<Expense>().Where(e => e.UserId == userId && e.CategoryId == categoryId).ToList();
        public Expense? GetExpenseById(int id) => Database.Table<Expense>().FirstOrDefault(e => e.Id == id);
        public int InsertExpense(Expense expense) => Database.Insert(expense);
        public int UpdateExpense(Expense expense) => Database.Update(expense);
        public int DeleteExpense(Expense expense) => Database.Delete(expense);
        #endregion

        #region Budgets
        public List<Budget> GetBudgetsByUserId(int userId) => Database.Table<Budget>().Where(b => b.UserId == userId && b.IsActive).ToList();
        public Budget? GetBudgetById(int id) => Database.Table<Budget>().FirstOrDefault(b => b.Id == id);
        public int InsertBudget(Budget budget) => Database.Insert(budget);
        public int UpdateBudget(Budget budget) => Database.Update(budget);
        public int DeleteBudget(Budget budget) => Database.Delete(budget);
        #endregion

        #region FixedCharges
        public List<FixedCharge> GetFixedChargesByUserId(int userId) => Database.Table<FixedCharge>().Where(f => f.UserId == userId && f.IsActive).ToList();
        public FixedCharge? GetFixedChargeById(int id) => Database.Table<FixedCharge>().FirstOrDefault(f => f.Id == id);
        public int InsertFixedCharge(FixedCharge fixedCharge) => Database.Insert(fixedCharge);
        public int UpdateFixedCharge(FixedCharge fixedCharge) => Database.Update(fixedCharge);
        public int DeleteFixedCharge(FixedCharge fixedCharge) => Database.Delete(fixedCharge);
        #endregion

        #region AlertThresholds
        public List<AlertThreshold> GetAlertThresholdsByUserId(int userId) => Database.Table<AlertThreshold>().Where(a => a.UserId == userId && a.IsActive).ToList();
        public AlertThreshold? GetAlertThresholdById(int id) => Database.Table<AlertThreshold>().FirstOrDefault(a => a.Id == a.Id);
        public int InsertAlertThreshold(AlertThreshold alertThreshold) => Database.Insert(alertThreshold);
        public int UpdateAlertThreshold(AlertThreshold alertThreshold) => Database.Update(alertThreshold);
        public int DeleteAlertThreshold(AlertThreshold alertThreshold) => Database.Delete(alertThreshold);
        #endregion

        /// <summary>
        /// Ferme la connexion à la base de données
        /// </summary>
        public void Close()
        {
            _connection?.Close();
            _connection = null;
        }

        /// <summary>
        /// Supprime toutes les données utilisateur (pour suppression de compte - RGPD)
        /// Suppression en cascade selon les spécifications
        /// </summary>
        /// <param name="userId">ID de l'utilisateur à supprimer</param>
        public void DeleteAllUserData(int userId)
        {
            Database.RunInTransaction(() =>
            {
                // Supprimer dans l'ordre des dépendances
                Database.Execute("DELETE FROM AlertThresholds WHERE UserId = ?", userId);
                Database.Execute("DELETE FROM Expenses WHERE UserId = ?", userId);
                Database.Execute("DELETE FROM FixedCharges WHERE UserId = ?", userId);
                Database.Execute("DELETE FROM Budgets WHERE UserId = ?", userId);
                Database.Execute("DELETE FROM Users WHERE Id = ?", userId);
            });
        }
    }
}