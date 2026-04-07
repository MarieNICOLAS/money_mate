using MoneyMate.Services.Interfaces;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation MAUI du service de dialogue pour les ViewModels.
    /// </summary>
    public class DialogService : IDialogService
    {
        /// <summary>
        /// Affiche une alerte simple.
        /// </summary>
        public Task ShowAlertAsync(string title, string message, string cancel)
        {
            Page? page = ResolveCurrentPage();
            return page?.DisplayAlert(title, message, cancel) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Affiche une boîte de confirmation.
        /// </summary>
        public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            Page? page = ResolveCurrentPage();
            return page?.DisplayAlert(title, message, accept, cancel) ?? Task.FromResult(false);
        }

        /// <summary>
        /// Récupère la page active de l'application.
        /// </summary>
        private static Page? ResolveCurrentPage()
        {
            if (Shell.Current?.CurrentPage != null)
                return Shell.Current.CurrentPage;

            return Application.Current?.Windows.FirstOrDefault()?.Page;
        }
    }
}
