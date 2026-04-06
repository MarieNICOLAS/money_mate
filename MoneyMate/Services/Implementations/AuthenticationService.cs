using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Service d'authentification avec hashage BCrypt.
    /// Conforme aux exigences de sécurité du projet.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "EUR",
            "USD",
            "GBP",
            "CHF",
            "CAD"
        };

        private const int MIN_PASSWORD_LENGTH = 8;
        private readonly MoneyMateDbContext _dbContext;
        private User? _currentUser;

        /// <summary>
        /// Initialise une nouvelle instance du service d'authentification.
        /// </summary>
        public AuthenticationService()
        {
            _dbContext = DatabaseService.Instance;
        }

        /// <summary>
        /// Indique si un utilisateur est actuellement connecté.
        /// </summary>
        public bool IsAuthenticated => _currentUser != null;

        /// <summary>
        /// Authentifie un utilisateur avec email et mot de passe.
        /// </summary>
        /// <param name="email">Email de l'utilisateur.</param>
        /// <param name="password">Mot de passe en clair.</param>
        /// <returns>Résultat contenant l'utilisateur authentifié si succès.</returns>
        public async Task<ServiceResult<User>> LoginAsync(string email, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_INVALID_INPUT",
                            "L'email et le mot de passe sont requis.");
                    }

                    string normalizedEmail = NormalizeEmail(email);
                    var user = _dbContext.GetUserByEmail(normalizedEmail);

                    if (user == null)
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_INVALID_CREDENTIALS",
                            "Email ou mot de passe incorrect.");
                    }

                    if (!user.IsActive)
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_ACCOUNT_INACTIVE",
                            "Ce compte est désactivé.");
                    }

                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                    if (!isPasswordValid)
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_INVALID_CREDENTIALS",
                            "Email ou mot de passe incorrect.");
                    }

                    _currentUser = user;

                    return ServiceResult<User>.Success(
                        user,
                        "Connexion réussie.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur LoginAsync : {ex.Message}");

                    return ServiceResult<User>.Failure(
                        "AUTH_UNEXPECTED_ERROR",
                        "Une erreur est survenue lors de la connexion.");
                }
            });
        }

        /// <summary>
        /// Enregistre un nouvel utilisateur.
        /// </summary>
        /// <param name="email">Email de l'utilisateur.</param>
        /// <param name="password">Mot de passe en clair.</param>
        /// <param name="devise">Devise préférée.</param>
        /// <returns>Résultat contenant l'utilisateur créé si succès.</returns>
        public async Task<ServiceResult<User>> RegisterAsync(string email, string password, string devise = "EUR")
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_INVALID_INPUT",
                            "L'email et le mot de passe sont requis.");
                    }

                    string normalizedEmail = NormalizeEmail(email);
                    string normalizedCurrency = NormalizeCurrency(devise);

                    if (!ValidationHelper.IsValidEmail(normalizedEmail))
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_INVALID_EMAIL",
                            "Le format de l'email est invalide.");
                    }

                    if (!AllowedCurrencies.Contains(normalizedCurrency))
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_INVALID_CURRENCY",
                            "La devise sélectionnée est invalide.");
                    }

                    if (!ValidatePasswordStrength(password))
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_WEAK_PASSWORD",
                            "Le mot de passe ne respecte pas les critères de sécurité.");
                    }

                    if (_dbContext.GetUserByEmail(normalizedEmail) != null)
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_EMAIL_ALREADY_EXISTS",
                            "Cet email est déjà utilisé.");
                    }

                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

                    var newUser = new User
                    {
                        Email = normalizedEmail,
                        PasswordHash = passwordHash,
                        Devise = normalizedCurrency,
                        BudgetStartDay = 1,
                        Role = "User",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    int userId = _dbContext.InsertUser(newUser);
                    if (userId <= 0)
                    {
                        return ServiceResult<User>.Failure(
                            "AUTH_CREATE_USER_FAILED",
                            "Impossible de créer le compte utilisateur.");
                    }

                    newUser.Id = userId;

                    System.Diagnostics.Debug.WriteLine($"Utilisateur cree : {normalizedEmail} (ID: {userId})");

                    return ServiceResult<User>.Success(
                        newUser,
                        "Compte créé avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur RegisterAsync : {ex.Message}");

                    return ServiceResult<User>.Failure(
                        "AUTH_UNEXPECTED_ERROR",
                        "Une erreur est survenue lors de l'inscription.");
                }
            });
        }

        /// <summary>
        /// Déconnecte l'utilisateur actuel.
        /// </summary>
        public async Task LogoutAsync()
        {
            await Task.Run(() =>
            {
                _currentUser = null;
                System.Diagnostics.Debug.WriteLine("Utilisateur déconnecté");
            });
        }

        /// <summary>
        /// Récupère l'utilisateur actuellement connecté.
        /// </summary>
        /// <returns>L'utilisateur connecté ou null.</returns>
        public User? GetCurrentUser()
            => _currentUser;

        /// <summary>
        /// Change le mot de passe de l'utilisateur.
        /// </summary>
        /// <param name="userId">ID de l'utilisateur.</param>
        /// <param name="oldPassword">Ancien mot de passe.</param>
        /// <param name="newPassword">Nouveau mot de passe.</param>
        /// <returns>Résultat de l'opération.</returns>
        public async Task<ServiceResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userId <= 0 || string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
                    {
                        return ServiceResult.Failure(
                            "AUTH_INVALID_INPUT",
                            "Les informations saisies sont invalides.");
                    }

                    var user = _dbContext.GetUserById(userId);
                    if (user == null)
                    {
                        return ServiceResult.Failure(
                            "AUTH_USER_NOT_FOUND",
                            "Utilisateur introuvable.");
                    }

                    bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);
                    if (!isOldPasswordValid)
                    {
                        return ServiceResult.Failure(
                            "AUTH_INVALID_OLD_PASSWORD",
                            "L'ancien mot de passe est incorrect.");
                    }

                    if (!ValidatePasswordStrength(newPassword))
                    {
                        return ServiceResult.Failure(
                            "AUTH_WEAK_PASSWORD",
                            "Le nouveau mot de passe ne respecte pas les critères de sécurité.");
                    }

                    if (string.Equals(oldPassword, newPassword, StringComparison.Ordinal))
                    {
                        return ServiceResult.Failure(
                            "AUTH_SAME_PASSWORD",
                            "Le nouveau mot de passe doit être différent de l'ancien.");
                    }

                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

                    int updatedRows = _dbContext.UpdateUser(user);
                    if (updatedRows != 1)
                    {
                        return ServiceResult.Failure(
                            "AUTH_UPDATE_PASSWORD_FAILED",
                            "La mise à jour du mot de passe a échoué.");
                    }

                    if (_currentUser?.Id == user.Id)
                        _currentUser = user;

                    System.Diagnostics.Debug.WriteLine($"Mot de passe change pour l utilisateur {userId}");

                    return ServiceResult.Success("Mot de passe modifié avec succès.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur ChangePasswordAsync : {ex.Message}");

                    return ServiceResult.Failure(
                        "AUTH_UNEXPECTED_ERROR",
                        "Une erreur est survenue lors du changement de mot de passe.");
                }
            });
        }

        /// <summary>
        /// Valide la force d'un mot de passe.
        /// Exigences : 8 caractères minimum, 1 majuscule, 1 minuscule,
        /// 1 chiffre, 1 caractère spécial.
        /// </summary>
        /// <param name="password">Mot de passe à valider.</param>
        /// <returns>True si le mot de passe est valide.</returns>
        public bool ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MIN_PASSWORD_LENGTH)
                return false;

            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialChar = password.Any(character => !char.IsLetterOrDigit(character));

            return hasUpperCase
                && hasLowerCase
                && hasDigit
                && hasSpecialChar;
        }

        /// <summary>
        /// Normalise un email pour stockage et comparaison.
        /// </summary>
        /// <param name="email">Email brut.</param>
        /// <returns>Email normalisé.</returns>
        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();

        /// <summary>
        /// Normalise une devise.
        /// </summary>
        /// <param name="devise">Devise brute.</param>
        /// <returns>Devise normalisée.</returns>
        private static string NormalizeCurrency(string devise)
            => string.IsNullOrWhiteSpace(devise)
                ? "EUR"
                : devise.Trim().ToUpperInvariant();
    }
}
