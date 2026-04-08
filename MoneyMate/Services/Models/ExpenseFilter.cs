namespace MoneyMate.Services.Models
{
    /// <summary>
    /// Représente les critères de filtrage d'une recherche de dépenses.
    /// </summary>
    public class ExpenseFilter
    {
        /// <summary>
        /// Identifiant utilisateur propriétaire.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Date minimale incluse.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Date maximale incluse.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Catégorie optionnelle.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Inclure uniquement les charges fixes si true, uniquement les non fixes si false, sinon tout.
        /// </summary>
        public bool? IsFixedCharge { get; set; }

        /// <summary>
        /// Recherche textuelle sur la note.
        /// </summary>
        public string SearchTerm { get; set; } = string.Empty;

        /// <summary>
        /// Montant minimum inclus.
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Montant maximum inclus.
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        /// Nombre d'éléments à ignorer.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Nombre maximum d'éléments à retourner.
        /// </summary>
        public int Take { get; set; }
    }
}
