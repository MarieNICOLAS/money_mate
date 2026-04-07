using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
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

        private readonly MoneyMateDbContext _dbContext;

        public FixedChargeService()
        {
            _dbContext = DatabaseService.Instance;
        }

        public async Task<ServiceResult<List<FixedCharge>>> GetFixedChargesAsync(int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<FixedCharge>>.Failure("FIXED_CHARGE_INVALID_USER", "Utilisateur invalide.");

                    List<FixedCharge> fixedCharges = _dbContext.GetFixedChargesByUserId(userId);
                    return ServiceResult<List<FixedCharge>>.Success(fixedCharges);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetFixedChargesAsync : {ex.Message}");
                    return ServiceResult<List<FixedCharge>>.Failure("FIXED_CHARGE_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des charges fixes.");
                }
            });
        }

        public async Task<ServiceResult<FixedCharge>> GetFixedChargeByIdAsync(int fixedChargeId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (fixedChargeId <= 0 || userId <= 0)
                        return ServiceResult<FixedCharge>.Failure("FIXED_CHARGE_INVALID_INPUT", "Les informations demandées sont invalides.");

                    FixedCharge? fixedCharge = _dbContext.GetFixedChargeById(fixedChargeId, userId);
                    if (fixedCharge == null)
                        return ServiceResult<FixedCharge>.Failure("FIXED_CHARGE_NOT_FOUND", "Charge fixe introuvable.");

                    return ServiceResult<FixedCharge>.Success(fixedCharge);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetFixedChargeByIdAsync : {ex.Message}");
                    return ServiceResult<FixedCharge>.Failure("FIXED_CHARGE_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement de la charge fixe.");
                }
            });
        }

        public async Task<ServiceResult<List<FixedCharge>>> GetUpcomingFixedChargesAsync(int userId, DateTime untilDate)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0)
                        return ServiceResult<List<FixedCharge>>.Failure("FIXED_CHARGE_INVALID_USER", "Utilisateur invalide.");

                    if (untilDate < DateTime.Now.Date)
                        return ServiceResult<List<FixedCharge>>.Failure("FIXED_CHARGE_INVALID_DATE", "La date de recherche est invalide.");

                    List<FixedCharge> fixedCharges = _dbContext.GetFixedChargesByUserId(userId)
                        .Where(fixedCharge => fixedCharge.IsActive)
                        .Where(fixedCharge => !fixedCharge.EndDate.HasValue || fixedCharge.EndDate.Value.Date >= DateTime.Now.Date)
                        .Where(fixedCharge => fixedCharge.GetNextOccurrenceDate().Date <= untilDate.Date)
                        .ToList();

                    return ServiceResult<List<FixedCharge>>.Success(fixedCharges);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur GetUpcomingFixedChargesAsync : {ex.Message}");
                    return ServiceResult<List<FixedCharge>>.Failure("FIXED_CHARGE_UNEXPECTED_ERROR", "Une erreur est survenue lors du chargement des prochaines charges fixes.");
                }
            });
        }

        public async Task<ServiceResult<FixedCharge>> CreateFixedChargeAsync(FixedCharge fixedCharge)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(fixedCharge);

                    ServiceResult validationResult = ValidateFixedCharge(fixedCharge);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<FixedCharge>.Failure(validationResult.ErrorCode, validationResult.Message);

                    fixedCharge.Name = fixedCharge.Name.Trim();
                    fixedCharge.Description = fixedCharge.Description?.Trim() ?? string.Empty;
                    fixedCharge.Frequency = fixedCharge.Frequency.Trim();
                    fixedCharge.CreatedAt = DateTime.UtcNow;
                    fixedCharge.IsActive = true;

                    int fixedChargeId = _dbContext.InsertFixedCharge(fixedCharge);
                    if (fixedChargeId <= 0)
                        return ServiceResult<FixedCharge>.Failure("FIXED_CHARGE_CREATE_FAILED", "Impossible de créer la charge fixe.");

                    fixedCharge.Id = fixedChargeId;
                    return ServiceResult<FixedCharge>.Success(fixedCharge, "Charge fixe créée avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur CreateFixedChargeAsync : {ex.Message}");
                    return ServiceResult<FixedCharge>.Failure("FIXED_CHARGE_UNEXPECTED_ERROR", "Une erreur est survenue lors de la création de la charge fixe.");
                }
            });
        }

        public async Task<ServiceResult<FixedCharge>> UpdateFixedChargeAsync(FixedCharge fixedCharge)
        {
            return await Task.Run(() =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(fixedCharge);

                    if (fixedCharge.Id <= 0)
                        return ServiceResult<FixedCharge>.Failure("FIXED_CHARGE_INVALID_ID", "La charge fixe à modifier est invalide.");

                    ServiceResult validationResult = ValidateFixedCharge(fixedCharge);
                    if (!validationResult.IsSuccess)
                        return ServiceResult<FixedCharge>.Failure(validationResult.ErrorCode, validationResult.Message);

                    fixedCharge.Name = fixedCharge.Name.Trim();
                    fixedCharge.Description = fixedCharge.Description?.Trim() ?? string.Empty;
                    fixedCharge.Frequency = fixedCharge.Frequency.Trim();

                    int updatedRows = _dbContext.UpdateFixedCharge(fixedCharge);
                    if (updatedRows != 1)
                        return ServiceResult<FixedCharge>.Failure("FIXED_CHARGE_UPDATE_FAILED", "La mise à jour de la charge fixe a échoué.");

                    return ServiceResult<FixedCharge>.Success(fixedCharge, "Charge fixe mise à jour avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur UpdateFixedChargeAsync : {ex.Message}");
                    return ServiceResult<FixedCharge>.Failure("FIXED_CHARGE_UNEXPECTED_ERROR", "Une erreur est survenue lors de la mise à jour de la charge fixe.");
                }
            });
        }

        public async Task<ServiceResult> DeleteFixedChargeAsync(int fixedChargeId, int userId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (fixedChargeId <= 0 || userId <= 0)
                        return ServiceResult.Failure("FIXED_CHARGE_INVALID_INPUT", "Les informations demandées sont invalides.");

                    FixedCharge? fixedCharge = _dbContext.GetFixedChargeById(fixedChargeId, userId);
                    if (fixedCharge == null)
                        return ServiceResult.Failure("FIXED_CHARGE_NOT_FOUND", "Charge fixe introuvable.");

                    int deletedRows = _dbContext.DeleteFixedCharge(fixedCharge);
                    if (deletedRows != 1)
                        return ServiceResult.Failure("FIXED_CHARGE_DELETE_FAILED", "La suppression de la charge fixe a échoué.");

                    return ServiceResult.Success("Charge fixe supprimée avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur DeleteFixedChargeAsync : {ex.Message}");
                    return ServiceResult.Failure("FIXED_CHARGE_UNEXPECTED_ERROR", "Une erreur est survenue lors de la suppression de la charge fixe.");
                }
            });
        }

        /// <summary>
        /// Valide les données métier d'une charge fixe.
        /// </summary>
        private static ServiceResult ValidateFixedCharge(FixedCharge fixedCharge)
        {
            if (fixedCharge.UserId <= 0)
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_USER", "Utilisateur invalide.");

            if (string.IsNullOrWhiteSpace(fixedCharge.Name))
                return ServiceResult.Failure("FIXED_CHARGE_NAME_REQUIRED", "Le nom de la charge fixe est requis.");

            if (fixedCharge.CategoryId <= 0)
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_CATEGORY", "Catégorie invalide.");

            if (!ValidationHelper.IsValidAmount(fixedCharge.Amount))
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_AMOUNT", "Le montant doit être strictement positif.");

            if (string.IsNullOrWhiteSpace(fixedCharge.Frequency) || !AllowedFrequencies.Contains(fixedCharge.Frequency.Trim()))
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_FREQUENCY", "La fréquence de récurrence est invalide.");

            if (fixedCharge.DayOfMonth < 1 || fixedCharge.DayOfMonth > 31)
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_DAY", "Le jour du mois doit être compris entre 1 et 31.");

            if (fixedCharge.EndDate.HasValue && fixedCharge.StartDate > fixedCharge.EndDate.Value)
                return ServiceResult.Failure("FIXED_CHARGE_INVALID_PERIOD", "La période de la charge fixe est invalide.");

            return ServiceResult.Success();
        }
    }
}
