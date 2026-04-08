using MoneyMate.Models;

namespace MoneyMate.Data.Context
{
    /// <summary>
    /// Contrat minimal du contexte de données pour les services métier.
    /// </summary>
    public interface IMoneyMateDbContext
    {
        List<Category> GetCategoriesByUserId(int userId);
        List<Category> GetCustomCategoriesByUserId(int userId);
        Category? GetCategoryById(int id);
        Category? GetCategoryById(int id, int userId);
        int InsertCategory(Category category);
        int UpdateCategory(Category category);
        int DeleteCategory(Category category);

        List<Expense> GetExpensesByUserId(int userId);
        List<Expense> GetExpensesByCategory(int userId, int categoryId);
        Expense? GetExpenseById(int id);
        Expense? GetExpenseById(int id, int userId);
        int InsertExpense(Expense expense);
        int UpdateExpense(Expense expense);
        int DeleteExpense(Expense expense);

        List<Budget> GetBudgetsByUserId(int userId);
        Budget? GetBudgetById(int id);
        Budget? GetBudgetById(int id, int userId);
        int InsertBudget(Budget budget);
        int UpdateBudget(Budget budget);
        int DeleteBudget(Budget budget);

        List<FixedCharge> GetFixedChargesByUserId(int userId);
        FixedCharge? GetFixedChargeById(int id);
        FixedCharge? GetFixedChargeById(int id, int userId);
        int InsertFixedCharge(FixedCharge fixedCharge);
        int UpdateFixedCharge(FixedCharge fixedCharge);
        int DeleteFixedCharge(FixedCharge fixedCharge);

        List<AlertThreshold> GetAlertThresholdsByUserId(int userId);
        AlertThreshold? GetAlertThresholdById(int id);
        AlertThreshold? GetAlertThresholdById(int id, int userId);
        int InsertAlertThreshold(AlertThreshold alertThreshold);
        int UpdateAlertThreshold(AlertThreshold alertThreshold);
        int DeleteAlertThreshold(AlertThreshold alertThreshold);

        User? GetUserById(int id);
        User? GetUserByEmail(string email);
        int InsertUser(User user);
        int UpdateUser(User user);
        void DeleteAllUserData(int userId);
        void Close();
    }
}
