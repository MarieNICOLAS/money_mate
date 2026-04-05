namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service de navigation pour l architecture MVVM
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigue vers une route specifiee
        /// </summary>
        Task NavigateToAsync(string route);

        /// <summary>
        /// Navigue vers une route avec des param�tres
        /// </summary>
        Task NavigateToAsync(string route, Dictionary<string, object> parameters);

        /// <summary>
        /// Retour en arri�re
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// Navigue vers la route principale (Dashboard)
        /// </summary>
        Task NavigateToMainAsync();

        /// <summary>
        /// Affiche une page modale
        /// </summary>
        Task PresentModalAsync(string route);

        /// <summary>
        /// Ferme la page modale actuelle
        /// </summary>
        Task DismissModalAsync();
    }
}