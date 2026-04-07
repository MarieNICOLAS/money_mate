namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service d'infrastructure UI pour l'affichage de dialogues applicatifs.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Affiche une alerte simple.
        /// </summary>
        Task ShowAlertAsync(string title, string message, string cancel);

        /// <summary>
        /// Affiche une boîte de confirmation.
        /// </summary>
        Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);
    }
}
