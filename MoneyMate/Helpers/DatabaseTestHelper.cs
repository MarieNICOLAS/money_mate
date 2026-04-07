using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Implementations;

namespace MoneyMate.Helpers
{
    /// <summary>
    /// Classe utilitaire pour tester la configuration de la base de données.
    /// </summary>
    public static class DatabaseTestHelper
    {
        /// <summary>
        /// Affiche le chemin exact de la base de données.
        /// </summary>
        public static void ShowDatabasePath()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbPath = Path.Combine(localAppData, "MoneyMate.db3");

                System.Diagnostics.Debug.WriteLine("=== CHEMIN BASE DE DONNEES ===");
                System.Diagnostics.Debug.WriteLine($"Chemin complet : {dbPath}");
                System.Diagnostics.Debug.WriteLine($"Dossier parent : {localAppData}");
                System.Diagnostics.Debug.WriteLine($"Fichier existe : {File.Exists(dbPath)}");

                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    System.Diagnostics.Debug.WriteLine($"Taille : {fileInfo.Length} bytes");
                    System.Diagnostics.Debug.WriteLine($"Modifie le : {fileInfo.LastWriteTime}");
                }

                System.Diagnostics.Debug.WriteLine("=================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR CHEMIN DB : {ex.Message}");
            }
        }

        /// <summary>
        /// Lance l'ensemble des vérifications immédiates de la couche Data.
        /// </summary>
        public static bool TestDatabaseConfiguration()
            => RunImmediateChecksAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Vérifie :
        /// 1. création et lecture d'utilisateurs,
        /// 2. catégories système et personnalisées,
        /// 3. sécurité des filtres UserId,
        /// 4. cohérence CRUD,
        /// 5. suppression cascade,
        /// 6. robustesse des retours null / listes vides.
        /// </summary>
        public static async Task<bool> RunImmediateChecksAsync()
        {
            MoneyMateDbContext? db = null;
            int userId1 = 0;
            int userId2 = 0;

            try
            {
                WriteInfo("=== DEBUT TEST DATABASE ===");

                ShowDatabasePath();

                db = DatabaseService.Instance;
                var authService = new AuthenticationService(new SessionManager());

                string testEmail1 = $"test-db-1-{Guid.NewGuid():N}@moneymate.local";
                string testEmail2 = $"test-db-2-{Guid.NewGuid():N}@moneymate.local";
                const string testPassword = "TestPassword123!";

                WriteInfo("1. Creation de deux comptes de test...");
                var createdUser1 = await authService.RegisterAsync(testEmail1, testPassword);
                var createdUser2 = await authService.RegisterAsync(testEmail2, testPassword);

                if (!createdUser1.IsSuccess || !createdUser2.IsSuccess ||
                    createdUser1.Data == null || createdUser2.Data == null)
                {
                    WriteError("ECHEC : impossible de creer les utilisateurs de test.");
                    return false;
                }

                userId1 = createdUser1.Data.Id;
                userId2 = createdUser2.Data.Id;

                if (userId1 <= 0 || userId2 <= 0)
                {
                    WriteError($"ECHEC : IDs invalides ({userId1}, {userId2}).");
                    return false;
                }

                WriteSuccess($"OK : utilisateurs crees - IDs: {userId1}, {userId2}");

                WriteInfo("2. Verification lecture utilisateur + normalisation email...");
                var persistedUser1 = db.GetUserById(userId1);
                var persistedUser1ByEmail = db.GetUserByEmail(testEmail1.ToUpperInvariant());

                if (persistedUser1 == null || persistedUser1ByEmail == null)
                {
                    WriteError("ECHEC : utilisateur non relu apres insertion.");
                    return false;
                }

                WriteSuccess($"OK : utilisateur relu - {persistedUser1.Email}");

                WriteInfo("3. Verification categories systeme...");
                var systemCategories = db.GetCategories();

                if (systemCategories.Count == 0)
                {
                    WriteError("ECHEC : aucune categorie systeme disponible.");
                    return false;
                }

                WriteSuccess($"OK : {systemCategories.Count} categorie(s) systeme detectee(s).");

                WriteInfo("4. Creation de categories personnalisees par utilisateur...");
                var customCategoryUser1 = new Category
                {
                    UserId = userId1,
                    IsSystem = false,
                    Name = $"Perso U1 {Guid.NewGuid():N}".Substring(0, 17),
                    Description = "Categorie de test utilisateur 1",
                    Color = "#123456",
                    Icon = "tag",
                    DisplayOrder = 100
                };

                var customCategoryUser2 = new Category
                {
                    UserId = userId2,
                    IsSystem = false,
                    Name = $"Perso U2 {Guid.NewGuid():N}".Substring(0, 17),
                    Description = "Categorie de test utilisateur 2",
                    Color = "#654321",
                    Icon = "tag",
                    DisplayOrder = 101
                };

                int customCategoryUser1Id = db.InsertCategory(customCategoryUser1);
                int customCategoryUser2Id = db.InsertCategory(customCategoryUser2);

                if (customCategoryUser1Id <= 0 || customCategoryUser2Id <= 0)
                {
                    WriteError("ECHEC : insertion des categories personnalisees.");
                    return false;
                }

                customCategoryUser1.Id = customCategoryUser1Id;
                customCategoryUser2.Id = customCategoryUser2Id;

                var user1Categories = db.GetCategoriesByUserId(userId1);
                var user1CustomCategories = db.GetCustomCategoriesByUserId(userId1);

                bool user1SeesOwnCategory = user1Categories.Any(c => c.Id == customCategoryUser1Id);
                bool user1DoesNotSeeUser2Category = user1Categories.All(c => c.Id != customCategoryUser2Id);
                bool user1CustomOnlyContainsOwnCategory = user1CustomCategories.Any(c => c.Id == customCategoryUser1Id)
                    && user1CustomCategories.All(c => c.UserId == userId1 && !c.IsSystem);

                if (!user1SeesOwnCategory || !user1DoesNotSeeUser2Category || !user1CustomOnlyContainsOwnCategory)
                {
                    WriteError("ECHEC : filtres categories/UserId incorrects.");
                    return false;
                }

                WriteSuccess("OK : filtres categories/UserId valides.");

                WriteInfo("5. Verification CRUD Expense + filtres UserId...");
                var expense = new Expense
                {
                    UserId = userId1,
                    Amount = 25.50m,
                    CategoryId = customCategoryUser1Id,
                    Note = "Test de depense",
                    DateOperation = DateTime.UtcNow
                };

                int expenseId = db.InsertExpense(expense);
                if (expenseId <= 0)
                {
                    WriteError("ECHEC : insertion de depense.");
                    return false;
                }

                var persistedExpense = db.GetExpenseById(expenseId, userId1);
                var crossUserExpense = db.GetExpenseById(expenseId, userId2);

                if (persistedExpense == null || crossUserExpense != null)
                {
                    WriteError("ECHEC : filtre UserId incorrect sur Expense.");
                    return false;
                }

                persistedExpense.Note = "Test de depense modifie";
                int expenseUpdateRows = db.UpdateExpense(persistedExpense);
                var updatedExpense = db.GetExpenseById(expenseId, userId1);

                if (expenseUpdateRows != 1 || updatedExpense == null || updatedExpense.Note != "Test de depense modifie")
                {
                    WriteError("ECHEC : mise a jour Expense.");
                    return false;
                }

                int unauthorizedDeleteExpenseRows = db.DeleteExpense(new Expense
                {
                    Id = expenseId,
                    UserId = userId2
                });

                if (unauthorizedDeleteExpenseRows != 0 || db.GetExpenseById(expenseId, userId1) == null)
                {
                    WriteError("ECHEC : suppression cross-user Expense non protegee.");
                    return false;
                }

                WriteSuccess("OK : CRUD Expense et filtre UserId valides.");

                WriteInfo("6. Verification refus d'insertion avec CategoryId invalide...");
                int invalidExpenseId = db.InsertExpense(new Expense
                {
                    UserId = userId1,
                    Amount = 10.00m,
                    CategoryId = int.MaxValue,
                    Note = "Depense invalide",
                    DateOperation = DateTime.UtcNow
                });

                if (invalidExpenseId != 0)
                {
                    WriteError("ECHEC : insertion Expense acceptee avec CategoryId invalide.");
                    return false;
                }

                WriteSuccess("OK : insertion Expense invalide correctement refusee.");

                WriteInfo("7. Verification suppression cascade DeleteBudget...");
                var budgetToDelete = new Budget
                {
                    UserId = userId1,
                    CategoryId = customCategoryUser1Id,
                    Amount = 300.00m,
                    PeriodType = "Monthly",
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddMonths(1),
                    IsActive = true
                };

                int budgetToDeleteId = db.InsertBudget(budgetToDelete);
                if (budgetToDeleteId <= 0)
                {
                    WriteError("ECHEC : insertion Budget.");
                    return false;
                }

                budgetToDelete.Id = budgetToDeleteId;

                var alertBudgetOnly = new AlertThreshold
                {
                    UserId = userId1,
                    BudgetId = budgetToDeleteId,
                    ThresholdPercentage = 80,
                    AlertType = "Warning",
                    Message = "Alerte budget test",
                    IsActive = true,
                    SendNotification = false
                };

                int alertBudgetOnlyId = db.InsertAlertThreshold(alertBudgetOnly);
                if (alertBudgetOnlyId <= 0)
                {
                    WriteError("ECHEC : insertion AlertThreshold lie au budget.");
                    return false;
                }

                alertBudgetOnly.Id = alertBudgetOnlyId;

                int deleteBudgetRows = db.DeleteBudget(budgetToDelete);
                var deletedBudget = db.GetBudgetById(budgetToDeleteId, userId1);
                var deletedBudgetAlert = db.GetAlertThresholdById(alertBudgetOnlyId, userId1);

                if (deleteBudgetRows != 1 || deletedBudget != null || deletedBudgetAlert != null)
                {
                    WriteError("ECHEC : suppression cascade DeleteBudget incorrecte.");
                    return false;
                }

                WriteSuccess("OK : DeleteBudget supprime aussi les alertes liees.");

                WriteInfo("8. Verification suppression cascade DeleteCategory...");
                var expenseToCascadeDelete = new Expense
                {
                    UserId = userId1,
                    Amount = 50.00m,
                    CategoryId = customCategoryUser1Id,
                    Note = "Depense pour suppression cascade",
                    DateOperation = DateTime.UtcNow
                };

                int expenseToCascadeDeleteId = db.InsertExpense(expenseToCascadeDelete);
                if (expenseToCascadeDeleteId <= 0)
                {
                    WriteError("ECHEC : insertion Expense pour test cascade categorie.");
                    return false;
                }

                var budgetForCategoryDelete = new Budget
                {
                    UserId = userId1,
                    CategoryId = customCategoryUser1Id,
                    Amount = 450.00m,
                    PeriodType = "Monthly",
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddMonths(1),
                    IsActive = true
                };

                int budgetForCategoryDeleteId = db.InsertBudget(budgetForCategoryDelete);
                if (budgetForCategoryDeleteId <= 0)
                {
                    WriteError("ECHEC : insertion Budget pour test cascade categorie.");
                    return false;
                }

                var fixedChargeForCategoryDelete = new FixedCharge
                {
                    UserId = userId1,
                    Name = "Netflix Test",
                    Description = "Charge fixe de test",
                    Amount = 15.99m,
                    CategoryId = customCategoryUser1Id,
                    Frequency = "Monthly",
                    DayOfMonth = 5,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddMonths(6),
                    IsActive = true,
                    AutoCreateExpense = true
                };

                int fixedChargeForCategoryDeleteId = db.InsertFixedCharge(fixedChargeForCategoryDelete);
                if (fixedChargeForCategoryDeleteId <= 0)
                {
                    WriteError("ECHEC : insertion FixedCharge pour test cascade categorie.");
                    return false;
                }

                var alertForCategoryDelete = new AlertThreshold
                {
                    UserId = userId1,
                    BudgetId = budgetForCategoryDeleteId,
                    CategoryId = customCategoryUser1Id,
                    ThresholdPercentage = 90,
                    AlertType = "Critical",
                    Message = "Alerte categorie test",
                    IsActive = true,
                    SendNotification = false
                };

                int alertForCategoryDeleteId = db.InsertAlertThreshold(alertForCategoryDelete);
                if (alertForCategoryDeleteId <= 0)
                {
                    WriteError("ECHEC : insertion AlertThreshold pour test cascade categorie.");
                    return false;
                }

                int deleteCategoryRows = db.DeleteCategory(customCategoryUser1);

                bool categoryDeleted = db.GetCategoryById(customCategoryUser1Id, userId1) == null;
                bool categoryExpensesDeleted = db.GetExpensesByCategory(userId1, customCategoryUser1Id).Count == 0;
                bool categoryBudgetDeleted = db.GetBudgetById(budgetForCategoryDeleteId, userId1) == null;
                bool categoryFixedChargeDeleted = db.GetFixedChargeById(fixedChargeForCategoryDeleteId, userId1) == null;
                bool categoryAlertDeleted = db.GetAlertThresholdById(alertForCategoryDeleteId, userId1) == null;

                if (deleteCategoryRows != 1 ||
                    !categoryDeleted ||
                    !categoryExpensesDeleted ||
                    !categoryBudgetDeleted ||
                    !categoryFixedChargeDeleted ||
                    !categoryAlertDeleted)
                {
                    WriteError("ECHEC : suppression cascade DeleteCategory incorrecte.");
                    return false;
                }

                WriteSuccess("OK : DeleteCategory supprime les donnees liees.");

                WriteInfo("9. Verification robustesse des retours vides/null...");
                bool invalidUserCategoriesEmpty = db.GetCategoriesByUserId(0).Count == 0;
                bool invalidUserBudgetsEmpty = db.GetBudgetsByUserId(0).Count == 0;
                bool invalidExpenseNull = db.GetExpenseById(0) == null;
                bool invalidBudgetNull = db.GetBudgetById(0) == null;

                if (!invalidUserCategoriesEmpty || !invalidUserBudgetsEmpty || !invalidExpenseNull || !invalidBudgetNull)
                {
                    WriteError("ECHEC : retours null/listes vides incoherents.");
                    return false;
                }

                WriteSuccess("OK : retours null/listes vides coherents.");

                WriteInfo("10. Verification DeleteAllUserData...");
                var user2Expense = new Expense
                {
                    UserId = userId2,
                    Amount = 99.99m,
                    CategoryId = customCategoryUser2Id,
                    Note = "Depense user 2",
                    DateOperation = DateTime.UtcNow
                };

                int user2ExpenseId = db.InsertExpense(user2Expense);
                if (user2ExpenseId <= 0)
                {
                    WriteError("ECHEC : insertion depense user 2.");
                    return false;
                }

                var user2Budget = new Budget
                {
                    UserId = userId2,
                    CategoryId = customCategoryUser2Id,
                    Amount = 600.00m,
                    PeriodType = "Monthly",
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddMonths(1),
                    IsActive = true
                };

                int user2BudgetId = db.InsertBudget(user2Budget);
                if (user2BudgetId <= 0)
                {
                    WriteError("ECHEC : insertion budget user 2.");
                    return false;
                }

                var user2FixedCharge = new FixedCharge
                {
                    UserId = userId2,
                    Name = "Spotify Test",
                    Description = "Charge fixe user 2",
                    Amount = 10.99m,
                    CategoryId = customCategoryUser2Id,
                    Frequency = "Monthly",
                    DayOfMonth = 10,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddMonths(6),
                    IsActive = true,
                    AutoCreateExpense = true
                };

                int user2FixedChargeId = db.InsertFixedCharge(user2FixedCharge);
                if (user2FixedChargeId <= 0)
                {
                    WriteError("ECHEC : insertion fixed charge user 2.");
                    return false;
                }

                var user2Alert = new AlertThreshold
                {
                    UserId = userId2,
                    BudgetId = user2BudgetId,
                    CategoryId = customCategoryUser2Id,
                    ThresholdPercentage = 75,
                    AlertType = "Warning",
                    Message = "Alerte user 2",
                    IsActive = true,
                    SendNotification = false
                };

                int user2AlertId = db.InsertAlertThreshold(user2Alert);
                if (user2AlertId <= 0)
                {
                    WriteError("ECHEC : insertion alerte user 2.");
                    return false;
                }

                db.DeleteAllUserData(userId2);

                bool user2Deleted = db.GetUserById(userId2) == null;
                bool user2ExpensesDeleted = db.GetExpensesByUserId(userId2).Count == 0;
                bool user2BudgetsDeleted = db.GetBudgetsByUserId(userId2).Count == 0;
                bool user2FixedChargesDeleted = db.GetFixedChargesByUserId(userId2).Count == 0;
                bool user2AlertsDeleted = db.GetAlertThresholdsByUserId(userId2).Count == 0;
                bool user2CategoriesDeleted = db.GetCustomCategoriesByUserId(userId2).Count == 0;

                if (!user2Deleted ||
                    !user2ExpensesDeleted ||
                    !user2BudgetsDeleted ||
                    !user2FixedChargesDeleted ||
                    !user2AlertsDeleted ||
                    !user2CategoriesDeleted)
                {
                    WriteError("ECHEC : DeleteAllUserData n'a pas tout supprime.");
                    return false;
                }

                userId2 = 0;

                WriteSuccess("OK : DeleteAllUserData supprime toutes les donnees utilisateur.");
                WriteSuccess("TOUS LES TESTS ONT REUSSI.");
                WriteInfo("=== FIN TEST DATABASE ===");

                return true;
            }
            catch (Exception ex)
            {
                WriteError($"ERREUR TEST DB : {ex.Message}");
                WriteError($"StackTrace : {ex.StackTrace}");
                WriteError("=== ECHEC TEST DATABASE ===");
                return false;
            }
            finally
            {
                if (db != null)
                {
                    TryCleanupUser(db, userId1);
                    TryCleanupUser(db, userId2);
                }
            }
        }

        /// <summary>
        /// Affiche des statistiques simples sur la base de données.
        /// </summary>
        public static void ShowDatabaseStats()
        {
            try
            {
                var db = DatabaseService.Instance;
                var allUsers = db.GetUsers();

                System.Diagnostics.Debug.WriteLine("=== STATISTIQUES DATABASE ===");
                System.Diagnostics.Debug.WriteLine($"Utilisateurs: {allUsers.Count}");
                System.Diagnostics.Debug.WriteLine($"Categories systeme: {db.GetCategories().Count}");

                foreach (var user in allUsers)
                {
                    var customCategories = db.GetCustomCategoriesByUserId(user.Id);
                    var expenses = db.GetExpensesByUserId(user.Id);
                    var budgets = db.GetBudgetsByUserId(user.Id);
                    var fixedCharges = db.GetFixedChargesByUserId(user.Id);
                    var alerts = db.GetAlertThresholdsByUserId(user.Id);

                    System.Diagnostics.Debug.WriteLine(
                        $"{user.Email}: " +
                        $"{customCategories.Count} categorie(s) perso, " +
                        $"{expenses.Count} depense(s), " +
                        $"{budgets.Count} budget(s), " +
                        $"{fixedCharges.Count} charge(s) fixe(s), " +
                        $"{alerts.Count} alerte(s)");
                }

                System.Diagnostics.Debug.WriteLine("=== FIN STATISTIQUES ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERREUR STATS : {ex.Message}");
            }
        }

        /// <summary>
        /// Supprime un utilisateur de test si necessaire.
        /// </summary>
        /// <param name="db">Contexte base de donnees.</param>
        /// <param name="userId">Identifiant utilisateur.</param>
        private static void TryCleanupUser(MoneyMateDbContext db, int userId)
        {
            try
            {
                if (userId <= 0)
                    return;

                if (db.GetUserById(userId) == null)
                    return;

                db.DeleteAllUserData(userId);
                WriteInfo($"CLEANUP : utilisateur {userId} supprime.");
            }
            catch (Exception ex)
            {
                WriteError($"CLEANUP ECHEC user {userId} : {ex.Message}");
            }
        }

        /// <summary>
        /// Ecrit un message d'information dans la sortie debug.
        /// </summary>
        private static void WriteInfo(string message)
            => System.Diagnostics.Debug.WriteLine(message);

        /// <summary>
        /// Ecrit un message de succes dans la sortie debug.
        /// </summary>
        private static void WriteSuccess(string message)
            => System.Diagnostics.Debug.WriteLine(message);

        /// <summary>
        /// Ecrit un message d'erreur dans la sortie debug.
        /// </summary>
        private static void WriteError(string message)
            => System.Diagnostics.Debug.WriteLine(message);
    }
}
