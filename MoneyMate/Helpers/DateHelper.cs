namespace MoneyMate.Helpers
{
    /// <summary>
    /// Utilitaires de manipulation et formatage de dates.
    /// </summary>
    public static class DateHelper
    {
        /// <summary>
        /// Retourne le début de la période budgétaire selon le jour de départ configuré.
        /// Ex: si startDay = 5, en avril → 05/04
        /// </summary>
        public static DateTime GetBudgetPeriodStart(int startDay)
        {
            var today = DateTime.Today;
            var start = new DateTime(today.Year, today.Month, Math.Min(startDay, DateTime.DaysInMonth(today.Year, today.Month)));

            return today.Day >= startDay ? start : start.AddMonths(-1);
        }

        /// <summary>
        /// Retourne la fin de la période budgétaire.
        /// </summary>
        public static DateTime GetBudgetPeriodEnd(int startDay)
            => GetBudgetPeriodStart(startDay).AddMonths(1).AddDays(-1);

        /// <summary>
        /// Formate une date en libellé lisible.
        /// Ex: "Aujourd'hui", "Hier", "12 avril"
        /// </summary>
        public static string ToRelativeLabel(DateTime date)
        {
            var today = DateTime.Today;
            var diff = (today - date.Date).Days;

            return diff switch
            {
                0 => "Aujourd'hui",
                1 => "Hier",
                _ when diff < 7 => date.ToString("dddd"),
                _ => date.ToString("d MMMM")
            };
        }

        /// <summary>
        /// Retourne le nom du mois en français.
        /// Ex: DateTime(2025,4,1) → "Avril 2025"
        /// </summary>
        public static string ToMonthLabel(DateTime date)
            => date.ToString("MMMM yyyy");

        /// <summary>
        /// Vérifie si une date appartient à la période budgétaire courante.
        /// </summary>
        public static bool IsInCurrentBudgetPeriod(DateTime date, int startDay)
        {
            var start = GetBudgetPeriodStart(startDay);
            var end = GetBudgetPeriodEnd(startDay);
            return date.Date >= start.Date && date.Date <= end.Date;
        }
    }
}
