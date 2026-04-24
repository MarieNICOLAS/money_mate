using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Common;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation du service métier pour la gestion des charges fixes.
    /// </summary>
    public class FixedChargeService : IFixedChargeService
    {
        private static readonly HashSet<string> AllowedFrequencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "Monthly",
            "Quarterly",
            "Yearly"
        };

        private readonly IMoneyMateDbContext _dbContext;

        public FixedChargeService()
            : this(DbContextFactory.CreateDefault())
        {
        }

        public FixedChargeService(IMoneyMateDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public Task<ServiceResult<List<FixedCharge>>> GetFixedChargesAsync(int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<FixedCharge>>.Failure(
                            "FIXED_CHARGE_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    List<FixedCharge> fixedCharges = _dbContext.GetFixedChargesByUserId(userId);
                    return ServiceResult<List<FixedCharge>>.Success(fixedCharges);
                },
                operationName: nameof(GetFixedChargesAsync),
                fallbackErrorCode: "FIXED_CHARGE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des charges fixes.");
        }

        public Task<ServiceResult<FixedCharge>> SetFixedChargeActiveStateAsync(int fixedChargeId, int userId, bool isActive)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (fixedChargeId <= 0 || userId <= 0)
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    FixedCharge? fixedCharge = _dbContext.GetFixedChargeById(fixedChargeId, userId);
                    if (fixedCharge is null)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_NOT_FOUND",
                            "Charge fixe introuvable.");
                    }

                    if (fixedCharge.IsActive == isActive)
                        return ServiceResult<FixedCharge>.Success(fixedCharge);

                    fixedCharge.IsActive = isActive;

                    int updatedRows = _dbContext.UpdateFixedCharge(fixedCharge);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_UPDATE_FAILED",
                            "La mise à jour de la charge fixe a échoué.");
                    }

                    return ServiceResult<FixedCharge>.Success(
                        fixedCharge,
                        isActive
                            ? "Charge fixe activée avec succès."
                            : "Charge fixe désactivée avec succès.");
                },
                operationName: nameof(SetFixedChargeActiveStateAsync),
                fallbackErrorCode: "FIXED_CHARGE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour de la charge fixe.");
        }

        public Task<ServiceResult<List<Expense>>> GenerateExpensesUntilAsync(int userId, DateTime untilDate)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<Expense>>.Failure(
                            "FIXED_CHARGE_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    DateTime generationLimit = untilDate.Date;
                    if (generationLimit < DateTime.Now.Date)
                    {
                        return ServiceResult<List<Expense>>.Failure(
                            "FIXED_CHARGE_INVALID_DATE",
                            "La date limite de génération est invalide.");
                    }

                    List<FixedCharge> fixedCharges = _dbContext.GetFixedChargesByUserId(userId)
                        .Where(fixedCharge => fixedCharge.IsActive && fixedCharge.AutoCreateExpense)
                        .ToList();

                    List<Expense> generatedExpenses = [];
                    List<Expense> existingExpenses = _dbContext.GetExpensesByUserId(userId)
                        .Where(expense => expense.IsFixedCharge)
                        .ToList();

                    foreach (FixedCharge fixedCharge in fixedCharges)
                    {
                        foreach (DateTime occurrence in EnumerateOccurrences(fixedCharge, generationLimit))
                        {
                            bool alreadyGenerated = existingExpenses.Any(expense =>
                                expense.CategoryId == fixedCharge.CategoryId &&
                                expense.IsFixedCharge &&
                                expense.DateOperation.Date == occurrence.Date &&
                                expense.Amount == fixedCharge.Amount &&
                                string.Equals(expense.Note, fixedCharge.Name, StringComparison.OrdinalIgnoreCase));

                            if (alreadyGenerated)
                                continue;

                            Expense expense = new()
                            {
                                UserId = fixedCharge.UserId,
                                CategoryId = fixedCharge.CategoryId,
                                Amount = fixedCharge.Amount,
                                DateOperation = occurrence,
                                IsFixedCharge = true,
                                Note = fixedCharge.Name
                            };

                            int expenseId = _dbContext.InsertExpense(expense);
                            if (expenseId <= 0)
                                continue;

                            expense.Id = expenseId;
                            generatedExpenses.Add(expense);
                            existingExpenses.Add(expense);
                        }
                    }

                    return ServiceResult<List<Expense>>.Success(
                        generatedExpenses,
                        "Génération des dépenses récurrentes terminée.");
                },
                operationName: nameof(GenerateExpensesUntilAsync),
                fallbackErrorCode: "FIXED_CHARGE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la génération des dépenses récurrentes.");
        }

        public Task<ServiceResult<FixedCharge>> GetFixedChargeByIdAsync(int fixedChargeId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (fixedChargeId <= 0 || userId <= 0)
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    FixedCharge? fixedCharge = _dbContext.GetFixedChargeById(fixedChargeId, userId);
                    if (fixedCharge is null)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_NOT_FOUND",
                            "Charge fixe introuvable.");
                    }

                    return ServiceResult<FixedCharge>.Success(fixedCharge);
                },
                operationName: nameof(GetFixedChargeByIdAsync),
                fallbackErrorCode: "FIXED_CHARGE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement de la charge fixe.");
        }

        public Task<ServiceResult<List<FixedCharge>>> GetUpcomingFixedChargesAsync(int userId, DateTime untilDate)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (userId <= 0)
                        return ServiceResult<List<FixedCharge>>.Failure(
                            "FIXED_CHARGE_INVALID_USER",
                            ServiceMessages.InvalidUser);

                    if (untilDate < DateTime.Now.Date)
                    {
                        return ServiceResult<List<FixedCharge>>.Failure(
                            "FIXED_CHARGE_INVALID_DATE",
                            "La date de recherche est invalide.");
                    }

                    List<FixedCharge> fixedCharges = _dbContext.GetFixedChargesByUserId(userId)
                        .Where(fixedCharge => fixedCharge.IsActive)
                        .Where(fixedCharge => !fixedCharge.EndDate.HasValue || fixedCharge.EndDate.Value.Date >= DateTime.Now.Date)
                        .Where(fixedCharge => fixedCharge.GetNextOccurrenceDate().Date <= untilDate.Date)
                        .ToList();

                    return ServiceResult<List<FixedCharge>>.Success(fixedCharges);
                },
                operationName: nameof(GetUpcomingFixedChargesAsync),
                fallbackErrorCode: "FIXED_CHARGE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors du chargement des prochaines charges fixes.");
        }

        public Task<ServiceResult<FixedCharge>> CreateFixedChargeAsync(FixedCharge fixedCharge)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(fixedCharge);

                    ServiceResult validationResult = ValidateFixedCharge(fixedCharge);
                    if (!validationResult.IsSuccess)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);
                    }

                    if (!CategoryExistsForUser(fixedCharge.UserId, fixedCharge.CategoryId))
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_CATEGORY_NOT_FOUND",
                            "La catégorie sélectionnée est introuvable ou inactive.");
                    }

                    fixedCharge.Name = fixedCharge.Name.Trim();
                    fixedCharge.Description = fixedCharge.Description?.Trim() ?? string.Empty;
                    fixedCharge.Frequency = fixedCharge.Frequency.Trim();
                    fixedCharge.CreatedAt = DateTime.UtcNow;
                    fixedCharge.IsActive = true;

                    int fixedChargeId = _dbContext.InsertFixedCharge(fixedCharge);
                    if (fixedChargeId <= 0)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_CREATE_FAILED",
                            "Impossible de créer la charge fixe.");
                    }

                    fixedCharge.Id = fixedChargeId;
                    return ServiceResult<FixedCharge>.Success(fixedCharge, "Charge fixe créée avec succès.");
                },
                operationName: nameof(CreateFixedChargeAsync),
                fallbackErrorCode: "FIXED_CHARGE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la création de la charge fixe.");
        }

        public Task<ServiceResult<FixedCharge>> UpdateFixedChargeAsync(FixedCharge fixedCharge)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    ArgumentNullException.ThrowIfNull(fixedCharge);

                    if (fixedCharge.Id <= 0)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_INVALID_ID",
                            "La charge fixe à modifier est invalide.");
                    }

                    ServiceResult validationResult = ValidateFixedCharge(fixedCharge);
                    if (!validationResult.IsSuccess)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            validationResult.ErrorCode,
                            validationResult.Message);
                    }

                    FixedCharge? existingFixedCharge = _dbContext.GetFixedChargeById(fixedCharge.Id, fixedCharge.UserId);
                    if (existingFixedCharge is null)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_NOT_FOUND",
                            "Charge fixe introuvable.");
                    }

                    if (!CategoryExistsForUser(fixedCharge.UserId, fixedCharge.CategoryId))
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_CATEGORY_NOT_FOUND",
                            "La catégorie sélectionnée est introuvable ou inactive.");
                    }

                    fixedCharge.Name = fixedCharge.Name.Trim();
                    fixedCharge.Description = fixedCharge.Description?.Trim() ?? string.Empty;
                    fixedCharge.Frequency = fixedCharge.Frequency.Trim();

                    int updatedRows = _dbContext.UpdateFixedCharge(fixedCharge);
                    if (updatedRows != 1)
                    {
                        return ServiceResult<FixedCharge>.Failure(
                            "FIXED_CHARGE_UPDATE_FAILED",
                            "La mise à jour de la charge fixe a échoué.");
                    }

                    return ServiceResult<FixedCharge>.Success(fixedCharge, "Charge fixe mise à jour avec succès.");
                },
                operationName: nameof(UpdateFixedChargeAsync),
                fallbackErrorCode: "FIXED_CHARGE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la mise à jour de la charge fixe.");
        }

        public Task<ServiceResult> DeleteFixedChargeAsync(int fixedChargeId, int userId)
        {
            return ServiceExecution.ExecuteAsync(
                action: () =>
                {
                    if (fixedChargeId <= 0 || userId <= 0)
                        return ServiceResult.Failure(
                            "FIXED_CHARGE_INVALID_INPUT",
                            ServiceMessages.InvalidInput);

                    FixedCharge? fixedCharge = _dbContext.GetFixedChargeById(fixedChargeId, userId);
                    if (fixedCharge is null)
                    {
                        return ServiceResult.Failure(
                            "FIXED_CHARGE_NOT_FOUND",
                            "Charge fixe introuvable.");
                    }

                    int deletedRows = _dbContext.DeleteFixedCharge(fixedCharge);
                    if (deletedRows != 1)
                    {
                        return ServiceResult.Failure(
                            "FIXED_CHARGE_DELETE_FAILED",
                            "La suppression de la charge fixe a échoué.");
                    }

                    return ServiceResult.Success("Charge fixe supprimée avec succès.");
                },
                operationName: nameof(DeleteFixedChargeAsync),
                fallbackErrorCode: "FIXED_CHARGE_UNEXPECTED_ERROR",
                fallbackMessage: "Une erreur est survenue lors de la suppression de la charge fixe.");
        }

        private static ServiceResult ValidateFixedCharge(FixedCharge fixedCharge)
        {
            if (fixedCharge.UserId <= 0)
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_USER", ServiceMessages.InvalidUser);

            if (string.IsNullOrWhiteSpace(fixedCharge.Name))
                return ServiceResult.Failure("FIXED_CHARGE_NAME_REQUIRED", "Le nom de la charge fixe est requis.");

            if (fixedCharge.CategoryId <= 0)
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_CATEGORY", "Catégorie invalide.");

            if (!ValidationHelper.IsValidAmount(fixedCharge.Amount))
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_AMOUNT", "Le montant doit être strictement positif.");

            if (string.IsNullOrWhiteSpace(fixedCharge.Frequency) || !AllowedFrequencies.Contains(fixedCharge.Frequency.Trim()))
            {
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_FREQUENCY", "La fréquence de récurrence est invalide.");
            }

            if (fixedCharge.DayOfMonth < 1 || fixedCharge.DayOfMonth > 31)
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_DAY", "Le jour du mois doit être compris entre 1 et 31.");

            if (fixedCharge.EndDate.HasValue && fixedCharge.StartDate > fixedCharge.EndDate.Value)
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_PERIOD", "La période de la charge fixe est invalide.");

            return ServiceResult.Success();
        }

        private static IEnumerable<DateTime> EnumerateOccurrences(FixedCharge fixedCharge, DateTime untilDate)
        {
            DateTime occurrence = fixedCharge.StartDate.Date;
            DateTime maxDate = fixedCharge.EndDate?.Date ?? untilDate.Date;
            maxDate = maxDate > untilDate.Date ? untilDate.Date : maxDate;

            while (occurrence <= maxDate)
            {
                yield return occurrence;

                occurrence = fixedCharge.Frequency switch
                {
                    "Monthly" => occurrence.AddMonths(1),
                    "Quarterly" => occurrence.AddMonths(3),
                    "Yearly" => occurrence.AddYears(1),
                    _ => occurrence.AddMonths(1)
                };
            }
        }

        private bool CategoryExistsForUser(int userId, int categoryId)
        {
            Category? category = _dbContext.GetCategoryById(categoryId, userId);
            return category is not null && category.IsActive;
        }
    }
}
