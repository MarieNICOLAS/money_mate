namespace MoneyMate.Services.Results
{
    /// <summary>
    /// Représente le résultat d'une opération métier avec donnée de retour.
    /// </summary>
    /// <typeparam name="T">Type de la donnée retournée.</typeparam>
    public class ServiceResult<T> : ServiceResult
    {
        /// <summary>
        /// Donnée retournée par l'opération.
        /// Null en cas d'échec.
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        /// Crée un résultat de succès avec donnée.
        /// </summary>
        /// <param name="data">Donnée retournée.</param>
        /// <param name="message">Message optionnel.</param>
        /// <returns>Résultat réussi.</returns>
        public static ServiceResult<T> Success(T data, string message = "")
            => new()
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };

        /// <summary>
        /// Crée un résultat d'échec.
        /// </summary>
        /// <param name="errorCode">Code d'erreur.</param>
        /// <param name="message">Message associé.</param>
        /// <returns>Résultat en échec.</returns>
        public static new ServiceResult<T> Failure(string errorCode, string message)
            => new()
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                Message = message,
                Data = default
            };
    }
}
