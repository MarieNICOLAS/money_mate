namespace MoneyMate.Services.Models
{
    /// <summary>
    /// Représente le montant cumulé des dépenses pour une catégorie sur une période.
    /// </summary>
    public class DashboardCategorySpending
    {
        /// <summary>
        /// Identifiant de la catégorie.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Nom de la catégorie.
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Montant total dépensé.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Nombre de dépenses comptabilisées.
        /// </summary>
        public int ExpensesCount { get; set; }
    }
}
