using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Common;
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

        private readonly IMoneyMateDbContext _dbContext;

        public AlertThresholdService()
            : this(DatabaseService.Instance)
        {
        }

        public AlertThresholdService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public Task<ServiceResult<List<AlertThreshold>>> GetAlertThresholdsAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<AlertThreshold>>.Failure(
                            "ALERT_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    List<AlertThreshold> alertThresholds = _dbContext.GetAlertThresholdsByUserId(userId);
                    return ServiceResult<List<AlertThreshold>>.Success(alertThresholds);
                },
                operationName: nameof(GetAlertThresholdsAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des alertes.");
        }

        public Task<ServiceResult<AlertTriggerInfo>> EvaluateAlertAsync(int alertThresholdId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (alertThresholdId <= 0 || userId <= 0)
                        return ServiceResult<AlertTriggerInfo>.Failure(
                            "ALERT_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    AlertThreshold? alertThreshold = _dbContext.GetAlertThresholdById(alertThresholdId, userId);
                    if (alertThreshold is null)
                    {
                        return ServiceResult<AlertTriggerInfo>.Failure(
                            "ALERT_NOT_FOUND",
                            "Seuil d'alerte introuvable.");
                    }

                    return BuildAlertTriggerInfo(userId, alertThreshold);
                },
                operationName: nameof(EvaluateAlertAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de l'évaluation de l'alerte.");
        }

        public Task<ServiceResult<List<AlertThreshold>>> GetAlertThresholdsByTypeAsync(int userId, string alertType)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<AlertThreshold>>.Failure(
                            "ALERT_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    if (string.IsNullOrWhiteSpace(alertType) || !AllowedAlertTypes.Contains(alertType.Trim()))
                    {
                        return ServiceResult<List<AlertThreshold>>.Failure(
                            "ALERT_INVALID_TYPE",
                            "Le type d'alerte est invalide.");
                    }

                    string normalizedAlertType = alertType.Trim();

                    List<AlertThreshold> alertThresholds = _dbContext.GetAlertThresholdsByUserId(userId)
                        .Where(alert => string.Equals(alert.AlertType, normalizedAlertType, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    return ServiceResult<List<AlertThreshold>>.Success(alertThresholds);
                },
                operationName: nameof(GetAlertThresholdsByTypeAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des alertes.");
        }

        public Task<ServiceResult<AlertThreshold>> SetAlertThresholdActiveStateAsync(int alertThresholdId, int userId, bool isActive)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (alertThresholdId <= 0 || userId <= 0)
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    AlertThreshold? alertThreshold = _dbContext.GetAlertThresholdById(alertThresholdId, userId);
                    if (alertThreshold is null)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_NOT_FOUND",
                            "Seuil d'alerte introuvable.");
                    }

                    if (alertThreshold.IsActive == isActive)
                        return ServiceResult<AlertThreshold>.Success(alertThreshold);

                    alertThreshold.IsActive = isActive;

                    int updatedRows = _dbContext.UpdateAlertThreshold(alertThreshold);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_UPDATE_FAILED",
                            "La mise à jour du seuil d'alerte a échoué.");
                    }

                    return ServiceResult<AlertThreshold>.Success(
                        alertThreshold,
                        isActive
                            ? "Seuil d'alerte activé avec succès."
                            : "Seuil d'alerte désactivé avec succès.");
                },
                operationName: nameof(SetAlertThresholdActiveStateAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour du seuil d'alerte.");
        }

        public Task<ServiceResult<AlertThreshold>> GetAlertThresholdByIdAsync(int alertThresholdId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (alertThresholdId <= 0 || userId <= 0)
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    AlertThreshold? alertThreshold = _dbContext.GetAlertThresholdById(alertThresholdId, userId);
                    if (alertThreshold is null)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_NOT_FOUND",
                            "Seuil d'alerte introuvable.");
                    }

                    return ServiceResult<AlertThreshold>.Success(alertThreshold);
                },
                operationName: nameof(GetAlertThresholdByIdAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement du seuil d'alerte.");
        }

        public Task<ServiceResult<AlertThreshold>> CreateAlertThresholdAsync(AlertThreshold alertThreshold)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(alertThreshold);

                    ServiceResult validationResult = ValidateAlertThreshold(alertThreshold);
                    if (!validationResult.IsSuccess)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);
                    }

                    ServiceResult referencesValidationResult = ValidateAlertThresholdReferences(alertThreshold);
                    if (!referencesValidationResult.IsSuccess)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            referencesValidationResult.ErrorCode,
                            referencesValidationResult.Message);
                    }

                    alertThreshold.AlertType = alertThreshold.AlertType.Trim();
                    alertThreshold.Message = alertThreshold.Message?.Trim() ?? string.Empty;
                    alertThreshold.CreatedAt = DateTime.UtcNow;
                    alertThreshold.IsActive = true;

                    int alertThresholdId = _dbContext.InsertAlertThreshold(alertThreshold);
                    if (alertThresholdId <= 0)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_CREATE_FAILED",
                            "Impossible de créer le seuil d'alerte.");
                    }

                    alertThreshold.Id = alertThresholdId;
                    return ServiceResult<AlertThreshold>.Success(alertThreshold, "Seuil d'alerte créé avec succès.");
                },
                operationName: nameof(CreateAlertThresholdAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la création du seuil d'alerte.");
        }

        public Task<ServiceResult<AlertThreshold>> UpdateAlertThresholdAsync(AlertThreshold alertThreshold)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(alertThreshold);

                    if (alertThreshold.Id <= 0)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_INVALID_ID",
                            "Le seuil d'alerte à modifier est invalide.");
                    }

                    ServiceResult validationResult = ValidateAlertThreshold(alertThreshold);
                    if (!validationResult.IsSuccess)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);
                    }

                    AlertThreshold? existingAlertThreshold = _dbContext.GetAlertThresholdById(alertThreshold.Id, alertThreshold.UserId);
                    if (existingAlertThreshold is null)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_NOT_FOUND",
                            "Seuil d'alerte introuvable.");
                    }

                    ServiceResult referencesValidationResult = ValidateAlertThresholdReferences(alertThreshold);
                    if (!referencesValidationResult.IsSuccess)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            referencesValidationResult.ErrorCode,
                            referencesValidationResult.Message);
                    }

                    alertThreshold.AlertType = alertThreshold.AlertType.Trim();
                    alertThreshold.Message = alertThreshold.Message?.Trim() ?? string.Empty;

                    int updatedRows = _dbContext.UpdateAlertThreshold(alertThreshold);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<AlertThreshold>.Failure(
                            "ALERT_UPDATE_FAILED",
                            "La mise à jour du seuil d'alerte a échoué.");
                    }

                    return ServiceResult<AlertThreshold>.Success(alertThreshold, "Seuil d'alerte mis à jour avec succès.");
                },
                operationName: nameof(UpdateAlertThresholdAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour du seuil d'alerte.");
        }

        public Task<ServiceResult> DeleteAlertThresholdAsync(int alertThresholdId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (alertThresholdId <= 0 || userId <= 0)
                        return ServiceResult.Failure(
                            "ALERT_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    AlertThreshold? alertThreshold = _dbContext.GetAlertThresholdById(alertThresholdId, userId);
                    if (alertThreshold is null)
                    {
                        return ServiceResult.Failure(
                            "ALERT_NOT_FOUND",
                            "Seuil d'alerte introuvable.");
                    }

                    int deletedRows = _dbContext.DeleteAlertThreshold(alertThreshold);
                    if (deletedRows != 1)
                    {
                        return ServiceResult.Failure(
                            "ALERT_DELETE_FAILED",
                            "La suppression du seuil d'alerte a échoué.");
                    }

                    return ServiceResult.Success("Seuil d'alerte supprimé avec succès.");
                },
                operationName: nameof(DeleteAlertThresholdAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la suppression du seuil d'alerte.");
        }

        public Task<ServiceResult<List<AlertThreshold>>> GetTriggeredAlertsAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<AlertThreshold>>.Failure(
                            "ALERT_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    List<AlertThreshold> triggeredAlerts = _dbContext.GetAlertThresholdsByUserId(userId)
                        .Where(alertThreshold => IsTriggered(userId, alertThreshold))
                        .ToList();

                    return ServiceResult<List<AlertThreshold>>.Success(triggeredAlerts);
                },
                operationName: nameof(GetTriggeredAlertsAsync),
                fallbackErrorCode: "ALERT_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de l'évaluation des alertes.");
        }

        private static ServiceResult ValidateAlertThreshold(AlertThreshold alertThreshold)
        {
            if (alertThreshold.UserId <= 0)
                return ServiceResult.Failure("ALERT_INVALID_USER", ServiceMessages.InvalidUser);

            if (alertThreshold.ThresholdPercentage < 0 || alertThreshold.ThresholdPercentage > 100)
                return ServiceResult.Failure("ALERT_INVALID_THRESHOLD", "Le seuil doit être compris entre 0 et 100.");

            if (string.IsNullOrWhiteSpace(alertThreshold.AlertType) || !AllowedAlertTypes.Contains(alertThreshold.AlertType.Trim()))
                return ServiceResult.Failure("ALERT_INVALID_TYPE", "Le type d'alerte est invalide.");

            return ServiceResult.Success();
        }

        private ServiceResult ValidateAlertThresholdReferences(AlertThreshold alertThreshold)
        {
            if (!alertThreshold.BudgetId.HasValue && !alertThreshold.CategoryId.HasValue)
            {
                return ServiceResult.Failure(
                    "ALERT_TARGET_REQUIRED",
                    "Le seuil d'alerte doit cibler un budget ou une catégorie.");
            }

            if (alertThreshold.BudgetId.HasValue)
            {
                Budget? budget = _dbContext.GetBudgetById(alertThreshold.BudgetId.Value, alertThreshold.UserId);
                if (budget is null)
                {
                    return ServiceResult.Failure(
                        "ALERT_BUDGET_NOT_FOUND",
                        "Le budget sélectionné est introuvable.");
                }
            }

            if (alertThreshold.CategoryId.HasValue)
            {
                Category? category = _dbContext.GetCategoryById(alertThreshold.CategoryId.Value, alertThreshold.UserId);
                if (category is null || !category.IsActive)
                {
                    return ServiceResult.Failure(
                        "ALERT_CATEGORY_NOT_FOUND",
                        "La catégorie sélectionnée est introuvable ou inactive.");
                }
            }

            return ServiceResult.Success();
        }

        private bool IsTriggered(int userId, AlertThreshold alertThreshold)
        {
            ServiceResult<AlertTriggerInfo> result = BuildAlertTriggerInfo(userId, alertThreshold);
            return result.IsSuccess && result.Data is not null && result.Data.IsTriggered;
        }

        private Budget? ResolveBudget(int userId, AlertThreshold alertThreshold)
        {
            if (alertThreshold.BudgetId.HasValue)
                return _dbContext.GetBudgetById(alertThreshold.BudgetId.Value, userId);

            DateTime now = DateTime.Now;

            return _dbContext.GetBudgetsByUserId(userId)
                .Where(budget => budget.StartDate.Year == now.Year && budget.StartDate.Month == now.Month)
                .OrderByDescending(budget => budget.CreatedAt)
                .FirstOrDefault();
        }

        private ServiceResult<AlertTriggerInfo> BuildAlertTriggerInfo(int userId, AlertThreshold alertThreshold)
        {
            Budget? budget = ResolveBudget(userId, alertThreshold);
            if (budget is null || budget.Amount <= 0)
            {
                return ServiceResult<AlertTriggerInfo>.Failure(
                    "ALERT_BUDGET_NOT_FOUND",
                    "Aucun budget compatible n'a été trouvé pour ce seuil d'alerte.");
            }

            budget.NormalizeToMonthlyPeriod();
            DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

            IEnumerable<Expense> expenses = _dbContext.GetExpensesByUserId(userId)
                .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate);

            if (alertThreshold.CategoryId.HasValue)
                expenses = expenses.Where(expense => expense.CategoryId == alertThreshold.CategoryId.Value);

            decimal totalExpenses = expenses.Sum(expense => expense.Amount);
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
