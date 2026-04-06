namespace MoneyMate.Services.Results
{
    /// <summary>
    /// Représente le résultat d'une opération métier sans donnée de retour.
    /// </summary>
    public class ServiceResult
    {
        /// <summary>
        /// Indique si l'opération a réussi.
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Code d'erreur technique ou métier.
        /// Vide si l'opération a réussi.
        /// </summary>
        public string ErrorCode { get; init; } = string.Empty;

        /// <summary>
        /// Message exploitable par le ViewModel pour l'affichage utilisateur.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Crée un résultat de succès.
        /// </summary>
        /// <param name="message">Message optionnel.</param>
        /// <returns>Résultat réussi.</returns>
        public static ServiceResult Success(string message = "")
            => new()
            {
                IsSuccess = true,
                Message = message
            };

        /// <summary>
        /// Crée un résultat d'échec.
        /// </summary>
        /// <param name="errorCode">Code d'erreur.</param>
        /// <param name="message">Message associé.</param>
        /// <returns>Résultat en échec.</returns>
        public static ServiceResult Failure(string errorCode, string message)
            => new()
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                Message = message
            };
    }
}
