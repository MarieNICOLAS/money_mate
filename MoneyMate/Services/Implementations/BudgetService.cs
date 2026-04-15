using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Common;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour la gestion des budgets mensuels globaux.
    /// </summary>
    public class BudgetService : IBudgetService
    {
        private readonly IMoneyMateDbContext _dbContext;

        public BudgetService()
            : this(DatabaseService.Instance)
        {
        }

        public BudgetService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public Task<ServiceResult<List<Budget>>> GetBudgetsAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<Budget>>.Failure(
                            "BUDGET_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    List<Budget> budgets = _dbContext.GetBudgetsByUserId(userId);
                    return ServiceResult<List<Budget>>.Success(budgets);
                },
                operationName: nameof(GetBudgetsAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des budgets.");
        }

        public Task<ServiceResult<Budget>> GetBudgetByIdAsync(int budgetId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (budgetId <= 0 || userId <= 0)
                        return ServiceResult<Budget>.Failure(
                            "BUDGET_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget is null)
                        return ServiceResult<Budget>.Failure(
                            "BUDGET_NOT_FOUND",
                            "Budget introuvable.");

                    budget.NormalizeToMonthlyPeriod();
                    return ServiceResult<Budget>.Success(budget);
                },
                operationName: nameof(GetBudgetByIdAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement du budget.");
        }

        public Task<ServiceResult<Budget>> CreateBudgetAsync(Budget budget)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(budget);

                    budget.NormalizeToMonthlyPeriod();

                    ServiceResult validationResult = ValidateBudget(budget);
                    if (!validationResult.IsSuccess)
                    {
                        return ServiceResult<Budget>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);
                    }

                    ServiceResult<bool> conflictResult = HasActiveBudgetConflictInternal(budget);
                    if (!conflictResult.IsSuccess)
                    {
                        return ServiceResult<Budget>.Failure(
                            conflictResult.ErrorCode,
                            conflictResult.Message);
                    }

                    if (conflictResult.Data)
                    {
                        return ServiceResult<Budget>.Failure(
                            "BUDGET_CONFLICT",
                            "Un budget existe déjà pour ce mois.");
                    }

                    budget.CreatedAt = DateTime.UtcNow;
                    budget.IsActive = true;

                    int budgetId = _dbContext.InsertBudget(budget);
                    if (budgetId <= 0)
                    {
                        return ServiceResult<Budget>.Failure(
                            "BUDGET_CREATE_FAILED",
                            "Impossible de créer le budget.");
                    }

                    budget.Id = budgetId;
                    return ServiceResult<Budget>.Success(budget, "Budget créé avec succès.");
                },
                operationName: nameof(CreateBudgetAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la création du budget.");
        }

        public Task<ServiceResult<Budget>> UpdateBudgetAsync(Budget budget)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(budget);

                    if (budget.Id <= 0)
                    {
                        return ServiceResult<Budget>.Failure(
                            "BUDGET_INVALID_ID",
                            "Le budget à modifier est invalide.");
                    }

                    budget.NormalizeToMonthlyPeriod();

                    ServiceResult validationResult = ValidateBudget(budget);
                    if (!validationResult.IsSuccess)
                    {
                        return ServiceResult<Budget>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);
                    }

                    Budget? existingBudget = _dbContext.GetBudgetById(budget.Id, budget.UserId);
                    if (existingBudget is null)
                    {
                        return ServiceResult<Budget>.Failure(
                            "BUDGET_NOT_FOUND",
                            "Budget introuvable.");
                    }

                    existingBudget.NormalizeToMonthlyPeriod();

                    bool monthChanged =
                        existingBudget.StartDate.Year != budget.StartDate.Year ||
                        existingBudget.StartDate.Month != budget.StartDate.Month;

                    if (monthChanged)
                    {
                        ServiceResult<bool> conflictResult = HasActiveBudgetConflictInternal(budget, budget.Id);
                        if (!conflictResult.IsSuccess)
                        {
                            return ServiceResult<Budget>.Failure(
                                conflictResult.ErrorCode,
                                conflictResult.Message);
                        }

                        if (conflictResult.Data)
                        {
                            return ServiceResult<Budget>.Failure(
                                "BUDGET_CONFLICT",
                                "Un budget existe déjà pour ce mois.");
                        }
                    }

                    int updatedRows = _dbContext.UpdateBudget(budget);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<Budget>.Failure(
                            "BUDGET_UPDATE_FAILED",
                            "La mise à jour du budget a échoué.");
                    }

                    return ServiceResult<Budget>.Success(budget, "Budget mis à jour avec succès.");
                },
                operationName: nameof(UpdateBudgetAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour du budget.");
        }

        public Task<ServiceResult> DeleteBudgetAsync(int budgetId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (budgetId <= 0 || userId <= 0)
                        return ServiceResult.Failure(
                            "BUDGET_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget is null)
                    {
                        return ServiceResult.Failure(
                            "BUDGET_NOT_FOUND",
                            "Budget introuvable.");
                    }

                    int deletedRows = _dbContext.DeleteBudget(budget);
                    if (deletedRows != 1)
                    {
                        return ServiceResult.Failure(
                            "BUDGET_DELETE_FAILED",
                            "La suppression du budget a échoué.");
                    }

                    return ServiceResult.Success("Budget supprimé avec succès.");
                },
                operationName: nameof(DeleteBudgetAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la suppression du budget.");
        }

        public Task<ServiceResult<decimal>> GetConsumedAmountAsync(int budgetId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (budgetId <= 0 || userId <= 0)
                    {
                        return ServiceResult<decimal>.Failure(
                            "BUDGET_INVALID_INPUT",
                            ServiceMessages.InvalidInput);
                    }

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget is null)
                    {
                        return ServiceResult<decimal>.Failure(
                            "BUDGET_NOT_FOUND",
                            "Budget introuvable.");
                    }

                    budget.NormalizeToMonthlyPeriod();

                    DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

                    decimal consumedAmount = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate)
                        .Sum(expense => expense.Amount);

                    return ServiceResult<decimal>.Success(consumedAmount);
                },
                operationName: nameof(GetConsumedAmountAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du calcul du montant consommé.");
        }

        public Task<ServiceResult<decimal>> GetConsumedPercentageAsync(int budgetId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (budgetId <= 0 || userId <= 0)
                    {
                        return ServiceResult<decimal>.Failure(
                            "BUDGET_INVALID_INPUT",
                            ServiceMessages.InvalidInput);
                    }

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget is null)
                    {
                        return ServiceResult<decimal>.Failure(
                            "BUDGET_NOT_FOUND",
                            "Budget introuvable.");
                    }

                    budget.NormalizeToMonthlyPeriod();

                    DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

                    decimal consumedAmount = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate)
                        .Sum(expense => expense.Amount);

                    decimal percentage = budget.CalculateBudgetPercentage(consumedAmount);
                    return ServiceResult<decimal>.Success(percentage);
                },
                operationName: nameof(GetConsumedPercentageAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du calcul du pourcentage consommé.");
        }

        public Task<ServiceResult<BudgetConsumptionSummary>> GetBudgetConsumptionSummaryAsync(int budgetId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (budgetId <= 0 || userId <= 0)
                    {
                        return ServiceResult<BudgetConsumptionSummary>.Failure(
                            "BUDGET_INVALID_INPUT",
                            ServiceMessages.InvalidInput);
                    }

                    Budget? budget = _dbContext.GetBudgetById(budgetId, userId);
                    if (budget is null)
                    {
                        return ServiceResult<BudgetConsumptionSummary>.Failure(
                            "BUDGET_NOT_FOUND",
                            "Budget introuvable.");
                    }

                    budget.NormalizeToMonthlyPeriod();

                    DateTime endDate = budget.EndDate ?? DateTime.MaxValue;

                    decimal consumedAmount = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.DateOperation >= budget.StartDate && expense.DateOperation <= endDate)
                        .Sum(expense => expense.Amount);

                    decimal consumedPercentage = budget.CalculateBudgetPercentage(consumedAmount);
                    decimal remainingAmount = budget.Amount - consumedAmount;

                    BudgetConsumptionSummary summary = new()
                    {
                        Budget = budget,
                        ConsumedAmount = consumedAmount,
                        ConsumedPercentage = consumedPercentage,
                        RemainingAmount = remainingAmount,
                        IsExceeded = consumedAmount > budget.Amount
                    };

                    return ServiceResult<BudgetConsumptionSummary>.Success(summary);
                },
                operationName: nameof(GetBudgetConsumptionSummaryAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du calcul du résumé de consommation.");
        }

        public Task<ServiceResult<bool>> HasActiveBudgetConflictAsync(Budget budget, int? excludedBudgetId = null)
        {
            return ServiceExecution.ExecuteAsync(
                action: () => HasActiveBudgetConflictInternal(budget, excludedBudgetId),
                operationName: nameof(HasActiveBudgetConflictAsync),
                fallbackErrorCode: "BUDGET_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la vérification des conflits de budget.");
        }

        private static ServiceResult ValidateBudget(Budget budget)
        {
            if (budget.UserId <= 0)
                return ServiceResult.Failure("BUDGET_INVALID_USER", ServiceMessages.InvalidUser);

            if (!ValidationHelper.IsValidAmount(budget.Amount))
                return ServiceResult.Failure("BUDGET_INVALID_AMOUNT", "Le montant du budget doit être strictement positif.");

            if (budget.StartDate.Date > DateTime.Today)
                return ServiceResult.Failure("BUDGET_FUTURE_MONTH_NOT_ALLOWED", "Impossible de créer un budget pour un mois futur.");

            if (!string.Equals(budget.PeriodType, "Monthly", StringComparison.OrdinalIgnoreCase))
                return ServiceResult.Failure("BUDGET_INVALID_PERIOD_TYPE", "Seuls les budgets mensuels sont autorisés.");

            return ServiceResult.Success();
        }

        private ServiceResult<bool> HasActiveBudgetConflictInternal(Budget budget, int? excludedBudgetId = null)
        {
            ArgumentNullException.ThrowIfNull(budget);

            if (budget.UserId <= 0)
            {
                return ServiceResult<bool>.Failure(
                    "BUDGET_INVALID_INPUT",
                    "Les informations du budget sont invalides.");
            }

            DateTime targetMonth = new(budget.StartDate.Year, budget.StartDate.Month, 1);

            bool hasConflict = _dbContext.GetBudgetsByUserId(budget.UserId)
                .Where(existingBudget => !excludedBudgetId.HasValue || existingBudget.Id != excludedBudgetId.Value)
                .Any(existingBudget =>
                    existingBudget.StartDate.Year == targetMonth.Year &&
                    existingBudget.StartDate.Month == targetMonth.Month);

            return ServiceResult<bool>.Success(hasConflict);
        }
    }
}
