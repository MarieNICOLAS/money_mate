using MoneyMate.Models;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service métier pour la gestion des seuils d'alerte.
    /// </summary>
    public interface IAlertThresholdService
    {
        /// <summary>
        /// Retourne les seuils d'alerte actifs d'un utilisateur.
        /// </summary>
        Task<ServiceResult<List<AlertThreshold>>> GetAlertThresholdsAsync(int userId);

        /// <summary>
        /// Retourne un seuil d'alerte appartenant à un utilisateur.
        /// </summary>
        Task<ServiceResult<AlertThreshold>> GetAlertThresholdByIdAsync(int alertThresholdId, int userId);

        /// <summary>
        /// Crée un nouveau seuil d'alerte.
        /// </summary>
        Task<ServiceResult<AlertThreshold>> CreateAlertThresholdAsync(AlertThreshold alertThreshold);

        /// <summary>
        /// Met à jour un seuil d'alerte.
        /// </summary>
        Task<ServiceResult<AlertThreshold>> UpdateAlertThresholdAsync(AlertThreshold alertThreshold);

        /// <summary>
        /// Supprime un seuil d'alerte.
        /// </summary>
        Task<ServiceResult> DeleteAlertThresholdAsync(int alertThresholdId, int userId);

        /// <summary>
        /// Retourne les alertes dont le seuil est atteint.
        /// </summary>
        Task<ServiceResult<List<AlertThreshold>>> GetTriggeredAlertsAsync(int userId);

        /// <summary>
        /// Évalue un seuil d'alerte et retourne son état détaillé.
        /// </summary>
        Task<ServiceResult<AlertTriggerInfo>> EvaluateAlertAsync(int alertThresholdId, int userId);

        /// <summary>
        /// Retourne les alertes d'un type donné.
        /// </summary>
        Task<ServiceResult<List<AlertThreshold>>> GetAlertThresholdsByTypeAsync(int userId, string alertType);

        /// <summary>
        /// Active ou désactive un seuil d'alerte.
        /// </summary>
        Task<ServiceResult<AlertThreshold>> SetAlertThresholdActiveStateAsync(int alertThresholdId, int userId, bool isActive);
    }
}
