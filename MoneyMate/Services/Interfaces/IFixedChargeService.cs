using MoneyMate.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    /// <summary>
    /// Service métier pour la gestion des charges fixes.
    /// </summary>
    public interface IFixedChargeService
    {
        /// <summary>
        /// Retourne les charges fixes actives d'un utilisateur.
        /// </summary>
        Task<ServiceResult<List<FixedCharge>>> GetFixedChargesAsync(int userId);

        /// <summary>
        /// Retourne une charge fixe appartenant à un utilisateur.
        /// </summary>
        Task<ServiceResult<FixedCharge>> GetFixedChargeByIdAsync(int fixedChargeId, int userId);

        /// <summary>
        /// Retourne les prochaines charges fixes jusqu'à une date donnée.
        /// </summary>
        Task<ServiceResult<List<FixedCharge>>> GetUpcomingFixedChargesAsync(int userId, DateTime untilDate);

        /// <summary>
        /// Crée une nouvelle charge fixe.
        /// </summary>
        Task<ServiceResult<FixedCharge>> CreateFixedChargeAsync(FixedCharge fixedCharge);

        /// <summary>
        /// Met à jour une charge fixe.
        /// </summary>
        Task<ServiceResult<FixedCharge>> UpdateFixedChargeAsync(FixedCharge fixedCharge);

        /// <summary>
        /// Supprime une charge fixe.
        /// </summary>
        Task<ServiceResult> DeleteFixedChargeAsync(int fixedChargeId, int userId);
    }
}
