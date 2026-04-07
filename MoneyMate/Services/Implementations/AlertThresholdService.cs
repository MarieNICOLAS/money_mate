using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour la gestion des seuils d'alerte.
    /// </summary>
    public class AlertThresholdService : IAlertThresholdService
    {
        private static readonly HashSet<string> AllowedAlertTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Warning",
            "Critical"
        };

        private readonly MoneyMateDbContext _dbContext;

        public AlertThresholdService()
        {
            _dbContext = DatabaseService.Instance;
        }

        public async Task<ServiceResult<List<AlertThreshold>>> GetAlertThresholdsAsync(int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<AlertThreshold>>.Failure("ALERT_INVALID_USER", "Utilisateur invalide.");

                    List<AlertThreshold> alertThresholds = _dbContext.GetAlertThresholdsByUserId(userId);
                    return ServiceResult<List<AlertThreshold>>.Success(alertThresholds);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetAlertThresholdsAsync : {ex.Message}");
                    return ServiceResult<List<AlertThreshold>>.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des alertes.");
                }
            });
        }

        public async Task<ServiceResult<AlertTriggerInfo>> EvaluateAlertAsync(int alertThresholdId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (alertThresholdId <= 0 || userId <= 0)
                        return ServiceResult<AlertTriggerInfo>.Failure("ALERT_INVALID_INPUT", "Les informations demandées sont invalides.");

                    AlertThreshold? alertThreshold = _dbContext.GetAlertThresholdById(alertThresholdId, userId);
                    if (alertThreshold == null)
                        return ServiceResult<AlertTriggerInfo>.Failure("ALERT_NOT_FOUND", "Seuil d'alerte introuvable.");

                    return BuildAlertTriggerInfo(userId, alertThreshold);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur EvaluateAlertAsync : {ex.Message}");
                    return ServiceResult<AlertTriggerInfo>.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors de l'évaluation de l'alerte.");
                }
            });
        }

        public async Task<ServiceResult<List<AlertThreshold>>> GetAlertThresholdsByTypeAsync(int userId, string alertType)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<AlertThreshold>>.Failure("ALERT_INVALID_USER", "Utilisateur invalide.");

                    if (string.IsNullOrWhiteSpace(alertType) || !AllowedAlertTypes.Contains(alertType.Trim()))
                        return ServiceResult<List<AlertThreshold>>.Failure("ALERT_INVALID_TYPE", "Le type d'alerte est invalide.");

                    string normalizedAlertType = alertType.Trim();

                    List<AlertThreshold> alertThresholds = _dbContext.GetAlertThresholdsByUserId(userId)
                        .Where(alert => string.Equals(alert.AlertType, normalizedAlertType, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    return ServiceResult<List<AlertThreshold>>.Success(alertThresholds);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetAlertThresholdsByTypeAsync : {ex.Message}");
                    return ServiceResult<List<AlertThreshold>>.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des alertes.");
                }
            });
        }

        public async Task<ServiceResult<AlertThreshold>> SetAlertThresholdActiveStateAsync(int alertThresholdId, int userId, bool isActive)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (alertThresholdId <= 0 || userId <= 0)
                        return ServiceResult<AlertThreshold>.Failure("ALERT_INVALID_INPUT", "Les informations demandées sont invalides.");

                    AlertThreshold? alertThreshold = _dbContext.GetAlertThresholdById(alertThresholdId, userId);
                    if (alertThreshold == null)
                        return ServiceResult<AlertThreshold>.Failure("ALERT_NOT_FOUND", "Seuil d'alerte introuvable.");

                    if (alertThreshold.IsActive == isActive)
                        return ServiceResult<AlertThreshold>.Success(alertThreshold);

                    alertThreshold.IsActive = isActive;

                    int updatedRows = _dbContext.UpdateAlertThreshold(alertThreshold);
                    if (updatedRows != 1)
                        return ServiceResult<AlertThreshold>.Failure("ALERT_UPDATE_FAILED", "La mise à jour du seuil d'alerte a échoué.");

                    return ServiceResult<AlertThreshold>.Success(alertThreshold, isActive
                        ? "Seuil d'alerte activé avec succès."
                        : "Seuil d'alerte désactivé avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur SetAlertThresholdActiveStateAsync : {ex.Message}");
                    return ServiceResult<AlertThreshold>.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors de la mise à jour du seuil d'alerte.");
                }
            });
        }

        public async Task<ServiceResult<AlertThreshold>> GetAlertThresholdByIdAsync(int alertThresholdId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (alertThresholdId <= 0 || userId <= 0)
                        return ServiceResult<AlertThreshold>.Failure("ALERT_INVALID_INPUT", "Les informations demandées sont invalides.");

                    AlertThreshold? alertThreshold = _dbContext.GetAlertThresholdById(alertThresholdId, userId);
                    if (alertThreshold == null)
                        return ServiceResult<AlertThreshold>.Failure("ALERT_NOT_FOUND", "Seuil d'alerte introuvable.");

                    return ServiceResult<AlertThreshold>.Success(alertThreshold);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetAlertThresholdByIdAsync : {ex.Message}");
                    return ServiceResult<AlertThreshold>.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement du seuil d'alerte.");
                }
            });
        }

        public async Task<ServiceResult<AlertThreshold>> CreateAlertThresholdAsync(AlertThreshold alertThreshold)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(alertThreshold);

                    ServiceResult validationResult = ValidateAlertThreshold(alertThreshold);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<AlertThreshold>.Failure(validationResult.ErrorCode, validationResult.Message);

                    alertThreshold.AlertType = alertThreshold.AlertType.Trim();
                    alertThreshold.Message = alertThreshold.Message?.Trim() ?? string.Empty;
                    alertThreshold.CreatedAt = DateTime.UtcNow;
                    alertThreshold.IsActive = true;

                    int alertThresholdId = _dbContext.InsertAlertThreshold(alertThreshold);
                    if (alertThresholdId <= 0)
                        return ServiceResult<AlertThreshold>.Failure("ALERT_CREATE_FAILED", "Impossible de créer le seuil d'alerte.");

                    alertThreshold.Id = alertThresholdId;
                    return ServiceResult<AlertThreshold>.Success(alertThreshold, "Seuil d'alerte créé avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur CreateAlertThresholdAsync : {ex.Message}");
                    return ServiceResult<AlertThreshold>.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors de la création du seuil d'alerte.");
                }
            });
        }

        public async Task<ServiceResult<AlertThreshold>> UpdateAlertThresholdAsync(AlertThreshold alertThreshold)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(alertThreshold);

                    if (alertThreshold.Id <= 0)
                        return ServiceResult<AlertThreshold>.Failure("ALERT_INVALID_ID", "Le seuil d'alerte à modifier est invalide.");

                    ServiceResult validationResult = ValidateAlertThreshold(alertThreshold);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<AlertThreshold>.Failure(validationResult.ErrorCode, validationResult.Message);

                    alertThreshold.AlertType = alertThreshold.AlertType.Trim();
                    alertThreshold.Message = alertThreshold.Message?.Trim() ?? string.Empty;

                    int updatedRows = _dbContext.UpdateAlertThreshold(alertThreshold);
                    if (updatedRows != 1)
                        return ServiceResult<AlertThreshold>.Failure("ALERT_UPDATE_FAILED", "La mise à jour du seuil d'alerte a échoué.");

                    return ServiceResult<AlertThreshold>.Success(alertThreshold, "Seuil d'alerte mis à jour avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur UpdateAlertThresholdAsync : {ex.Message}");
                    return ServiceResult<AlertThreshold>.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors de la mise à jour du seuil d'alerte.");
                }
            });
        }

        public async Task<ServiceResult> DeleteAlertThresholdAsync(int alertThresholdId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (alertThresholdId <= 0 || userId <= 0)
                        return ServiceResult.Failure("ALERT_INVALID_INPUT", "Les informations demandées sont invalides.");

                    AlertThreshold? alertThreshold = _dbContext.GetAlertThresholdById(alertThresholdId, userId);
                    if (alertThreshold == null)
                        return ServiceResult.Failure("ALERT_NOT_FOUND", "Seuil d'alerte introuvable.");

                    int deletedRows = _dbContext.DeleteAlertThreshold(alertThreshold);
                    if (deletedRows != 1)
                        return ServiceResult.Failure("ALERT_DELETE_FAILED", "La suppression du seuil d'alerte a échoué.");

                    return ServiceResult.Success("Seuil d'alerte supprimé avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur DeleteAlertThresholdAsync : {ex.Message}");
                    return ServiceResult.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors de la suppression du seuil d'alerte.");
                }
            });
        }

        public async Task<ServiceResult<List<AlertThreshold>>> GetTriggeredAlertsAsync(int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<AlertThreshold>>.Failure("ALERT_INVALID_USER", "Utilisateur invalide.");

                    List<AlertThreshold> triggeredAlerts = _dbContext.GetAlertThresholdsByUserId(userId)
                        .Where(alertThreshold => IsTriggered(userId, alertThreshold))
                        .ToList();

                    return ServiceResult<List<AlertThreshold>>.Success(triggeredAlerts);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetTriggeredAlertsAsync : {ex.Message}");
                    return ServiceResult<List<AlertThreshold>>.Failure("ALERT_UNEXPECTED_ERROR", "Une erreur est survenue lors de l'évaluation des alertes.");
                }
            });
        }

        /// <summary>
        /// Valide les données métier d'un seuil d'alerte.
        /// </summary>
        private static ServiceResult ValidateAlertThreshold(AlertThreshold alertThreshold)
        {
            if (alertThreshold.UserId <= 0)
                return ServiceResult.Failure("ALERT_INVALID_USER", "Utilisateur invalide.");

            if (alertThreshold.ThresholdPercentage <= 0 || alertThreshold.ThresholdPercentage > 100)
                return ServiceResult.Failure("ALERT_INVALID_THRESHOLD", "Le seuil doit être compris entre 0 et 100.");

            if (string.IsNullOrWhiteSpace(alertThreshold.AlertType) || !AllowedAlertTypes.Contains(alertThreshold.AlertType.Trim()))
                return ServiceResult.Failure("ALERT_INVALID_TYPE", "Le type d'alerte est invalide.");

            return ServiceResult.Success();
        }

        /// <summary>
        /// Indique si une alerte a atteint son seuil de déclenchement.
        /// </summary>
        private bool IsTriggered(int userId, AlertThreshold alertThreshold)
        {
            ServiceResult<AlertTriggerInfo> result = BuildAlertTriggerInfo(userId, alertThreshold);
            return result.IsSuccess && result.Data != null && result.Data.IsTriggered;
        }

        /// <summary>
        /// Résout le budget applicable à une alerte.
        /// </summary>
        private Budget? ResolveBudget(int userId, AlertThreshold alertThreshold)
        {
            if (alertThreshold.BudgetId.HasValue)
                return _dbContext.GetBudgetById(alertThreshold.BudgetId.Value, userId);

            if (!alertThreshold.CategoryId.HasValue)
                return null;

            DateTime now = DateTime.Now;

            return _dbContext.GetBudgetsByUserId(userId)
                .Where(budget => budget.CategoryId == alertThreshold.CategoryId.Value)
                .Where(budget => budget.StartDate <= now)
                .Where(budget => !budget.EndDate.HasValue || budget.EndDate.Value >= now)
                .OrderByDescending(budget => budget.StartDate)
                .ThenByDescending(budget => budget.CreatedAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// Construit l'état détaillé d'un seuil d'alerte.
        /// </summary>
        private ServiceResult<AlertTriggerInfo> BuildAlertTriggerInfo(int userId, AlertThreshold alertThreshold)
        {
            Budget? budget = ResolveBudget(userId, alertThreshold);
            if (budget == null || budget.Amount <= 0)
                return ServiceResult<AlertTriggerInfo>.Failure("ALERT_BUDGET_NOT_FOUND", "Aucun budget compatible n'a été trouvé pour ce seuil d'alerte.");

            int categoryId = alertThreshold.CategoryId ?? budget.CategoryId;
            DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

            decimal totalExpenses = _dbContext.GetExpensesByCategory(userId, categoryId)
                .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate)
                .Sum(expense => expense.Amount);

            decimal percentage = budget.CalculateBudgetPercentage(totalExpenses);

            AlertTriggerInfo triggerInfo = new()
            {
                AlertThreshold = alertThreshold,
                Budget = budget,
                BudgetAmount = budget.Amount,
                ConsumedAmount = totalExpenses,
                ConsumedPercentage = percentage,
                IsTriggered = percentage >= alertThreshold.ThresholdPercentage
            };

            return ServiceResult<AlertTriggerInfo>.Success(triggerInfo);
        }
    }
}
