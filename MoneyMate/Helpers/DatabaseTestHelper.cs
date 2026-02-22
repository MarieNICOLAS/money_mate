using MoneyMate.Data.Context;
using MoneyMate.Models;

namespace MoneyMate.Helpers
{
    /// <summary>
    /// Classe utilitaire pour tester la configuration de la base de données
    /// </summary>
    public static class DatabaseTestHelper
    {
        /// <summary>
        /// Affiche le chemin exact de la base de données
        /// </summary>
        public static void ShowDatabasePath()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbPath = Path.Combine(localAppData, "MoneyMate.db3");
                
                System.Diagnostics.Debug.WriteLine("=== ?? CHEMIN BASE DE DONNÉES ===");
                System.Diagnostics.Debug.WriteLine($"?? Chemin complet : {dbPath}");
                System.Diagnostics.Debug.WriteLine($"?? Dossier parent : {localAppData}");
                System.Diagnostics.Debug.WriteLine($"?? Fichier existe : {File.Exists(dbPath)}");
                
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    System.Diagnostics.Debug.WriteLine($"?? Taille : {fileInfo.Length} bytes");
                    System.Diagnostics.Debug.WriteLine($"?? Modifié le : {fileInfo.LastWriteTime}");
                }
                System.Diagnostics.Debug.WriteLine("=================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? ERREUR CHEMIN DB : {ex.Message}");
            }
        }

        /// <summary>
        /// Test complet de la base de données
        /// </summary>
        public static bool TestDatabaseConfiguration()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ?? DÉBUT TEST DATABASE ===");
                
                // Affichage du chemin en premier
                ShowDatabasePath();
                
                var db = DatabaseService.Instance;
                
                // 1. Test de création des tables
                System.Diagnostics.Debug.WriteLine("?? Test création tables...");
                
                // 2. Test d'insertion d'un utilisateur de test
                System.Diagnostics.Debug.WriteLine("?? Test création utilisateur...");
                var testUser = new User
                {
                    Email = "test@moneymate.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                    Devise = "EUR",
                    BudgetStartDay = 1,
                    Role = "User"
                };
                
                var userId = db.InsertUser(testUser);
                System.Diagnostics.Debug.WriteLine($"? Utilisateur créé - ID: {userId}");
                
                // 3. Test des catégories par défaut
                System.Diagnostics.Debug.WriteLine("??? Test catégories...");
                var categories = db.GetCategories();
                System.Diagnostics.Debug.WriteLine($"? {categories.Count} catégories chargées");
                
                // 4. Test d'insertion d'une dépense
                System.Diagnostics.Debug.WriteLine("?? Test ajout dépense...");
                var testExpense = new Expense
                {
                    UserId = testUser.Id,
                    Amount = 25.50m,
                    CategoryId = categories.First().Id,
                    Note = "Test de dépense",
                    DateOperation = DateTime.Now
                };
                
                var expenseId = db.InsertExpense(testExpense);
                System.Diagnostics.Debug.WriteLine($"? Dépense créée - ID: {expenseId}");
                
                // 5. Test de récupération des données
                System.Diagnostics.Debug.WriteLine("?? Test récupération données...");
                var userExpenses = db.GetExpensesByUserId(testUser.Id);
                System.Diagnostics.Debug.WriteLine($"? {userExpenses.Count} dépense(s) trouvée(s)");
                
                // 6. Nettoyage des données de test
                System.Diagnostics.Debug.WriteLine("?? Nettoyage données test...");
                db.DeleteAllUserData(testUser.Id);
                System.Diagnostics.Debug.WriteLine("? Données test supprimées");
                
                System.Diagnostics.Debug.WriteLine("?? TOUS LES TESTS ONT RÉUSSI !");
                System.Diagnostics.Debug.WriteLine("=== ? FIN TEST DATABASE ===");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? ERREUR TEST DB : {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"? StackTrace : {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("=== ? ÉCHEC TEST DATABASE ===");
                return false;
            }
        }
        
        /// <summary>
        /// Affiche des statistiques sur la base de données
        /// </summary>
        public static void ShowDatabaseStats()
        {
            try
            {
                var db = DatabaseService.Instance;
                
                System.Diagnostics.Debug.WriteLine("=== ?? STATISTIQUES DATABASE ===");
                System.Diagnostics.Debug.WriteLine($"?? Utilisateurs: {db.GetUsers().Count}");
                System.Diagnostics.Debug.WriteLine($"??? Catégories: {db.GetCategories().Count}");
                
                var allUsers = db.GetUsers();
                foreach (var user in allUsers)
                {
                    var expenses = db.GetExpensesByUserId(user.Id);
                    var budgets = db.GetBudgetsByUserId(user.Id);
                    System.Diagnostics.Debug.WriteLine($"?? {user.Email}: {expenses.Count} dépenses, {budgets.Count} budgets");
                }
                System.Diagnostics.Debug.WriteLine("=== ?? FIN STATISTIQUES ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? ERREUR STATS : {ex.Message}");
            }
        }
    }
}