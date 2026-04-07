using MoneyMate.Services.Interfaces;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation MAUI du service de navigation basé sur Shell.
    /// </summary>
    public class NavigationService : INavigationService
    {
        /// <summary>
        /// Navigue vers une route spécifiée.
        /// </summary>
        public Task NavigateToAsync(string route)
            => Shell.Current.GoToAsync(route);

        /// <summary>
        /// Navigue vers une route avec paramètres.
        /// </summary>
        public Task NavigateToAsync(string route, Dictionary<string, object> parameters)
            => Shell.Current.GoToAsync(route, parameters);

        /// <summary>
        /// Retour en arrière.
        /// </summary>
        public Task GoBackAsync()
            => Shell.Current.GoToAsync("..");

        /// <summary>
        /// Navigue vers la route principale authentifiée.
        /// </summary>
        public Task NavigateToMainAsync()
            => Shell.Current.GoToAsync("//DashboardPage");

        /// <summary>
        /// Affiche une page modale via Shell.
        /// </summary>
        public Task PresentModalAsync(string route)
            => Shell.Current.GoToAsync(route);

        /// <summary>
        /// Ferme la page modale actuelle.
        /// </summary>
        public Task DismissModalAsync()
            => Shell.Current.GoToAsync("..");
    }
}
