using MoneyMate.Data.Context;
using MoneyMate.Helpers;
using MoneyMate.Models;
using MoneyMate.Services.Common;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Implementations
{
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

        private const int MinPasswordLength = 8;

        private readonly IMoneyMateDbContext _dbContext;
        private readonly ISessionManager _sessionManager;

        public event EventHandler? AuthenticationStateChanged
        {
            add => _sessionManager.SessionChanged += value;
            remove => _sessionManager.SessionChanged -= value;
        }

        public AuthenticationService(ISessionManager sessionManager)
            : this(DatabaseService.Instance, sessionManager)
        {
        }

        public AuthenticationService(IMoneyMateDbContext dbContext, ISessionManager sessionManager)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        public bool IsAuthenticated => _sessionManager.IsAuthenticated;

        public User? GetCurrentUser() => _sessionManager.CurrentUser;

        public Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default)
            => _sessionManager.RestoreSessionAsync(cancellationToken);

        public string GetRememberedEmail() => _sessionManager.GetRememberedEmail();

        public bool GetRememberMePreference() => _sessionManager.GetRememberMePreference();

        public bool HasRole(params string[] roles) => _sessionManager.HasRole(roles);

        public bool CanAccessRoute(string route) => _sessionManager.CanAccessRoute(route);

        public async Task<ServiceResult<User>> LoginAsync(string email, string password, bool rememberSession = false)
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

                User? user = await Task.Run(() => _dbContext.GetUserByEmail(normalizedEmail));

                if (user is null)
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

                bool isPasswordValid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));

                if (!isPasswordValid)
                {
                    return ServiceResult<User>.Failure(
                        "AUTH_INVALID_CREDENTIALS",
                        "Email ou mot de passe incorrect.");
                }

                _sessionManager.StartSession(user, rememberSession);

                return ServiceResult<User>.Success(user, "Connexion réussie.");
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Erreur LoginAsync : {ex}");
#endif
                return ServiceResult<User>.Failure(
                    "AUTH_UNEXPECTED_ERROR",
                    "Une erreur est survenue lors de la connexion.");
            }
        }

        public async Task<ServiceResult<User>> RegisterAsync(string email, string password, string devise = "EUR")
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

                User? existingUser = await Task.Run(() => _dbContext.GetUserByEmail(normalizedEmail));
                if (existingUser is not null)
                {
                    return ServiceResult<User>.Failure(
                        "AUTH_EMAIL_ALREADY_EXISTS",
                        "Cet email est déjà utilisé.");
                }

                string passwordHash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12));

                User newUser = new()
                {
                    Email = normalizedEmail,
                    PasswordHash = passwordHash,
                    Devise = normalizedCurrency,
                    BudgetStartDay = 1,
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                int userId = await Task.Run(() => _dbContext.InsertUser(newUser));

                if (userId <= 0)
                {
                    return ServiceResult<User>.Failure(
                        "AUTH_CREATE_USER_FAILED",
                        "Impossible de créer le compte utilisateur.");
                }

                newUser.Id = userId;

                return ServiceResult<User>.Success(newUser, "Compte créé avec succès.");
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Erreur RegisterAsync : {ex}");
#endif
                return ServiceResult<User>.Failure(
                    "AUTH_UNEXPECTED_ERROR",
                    "Une erreur est survenue lors de l'inscription.");
            }
        }

        public Task LogoutAsync(bool clearPersistentSession = true)
        {
            _sessionManager.ClearSession(clearPersistentSession);
            return Task.CompletedTask;
        }

        public async Task<ServiceResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            try
            {
                if (userId <= 0 || string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
                {
                    return ServiceResult.Failure(
                        "AUTH_INVALID_INPUT",
                        "Les informations saisies sont invalides.");
                }

                User? user = await Task.Run(() => _dbContext.GetUserById(userId));
                if (user is null)
                {
                    return ServiceResult.Failure(
                        "AUTH_USER_NOT_FOUND",
                        "Utilisateur introuvable.");
                }

                bool isOldPasswordValid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash));
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

                user.PasswordHash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12));

                int updatedRows = await Task.Run(() => _dbContext.UpdateUser(user));
                if (updatedRows != 1)
                {
                    return ServiceResult.Failure(
                        "AUTH_UPDATE_PASSWORD_FAILED",
                        "La mise à jour du mot de passe a échoué.");
                }

                if (_sessionManager.CurrentUser?.Id == user.Id)
                    _sessionManager.UpdateCurrentUser(user);

                return ServiceResult.Success("Mot de passe modifié avec succès.");
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Erreur ChangePasswordAsync : {ex}");
#endif
                return ServiceResult.Failure(
                    "AUTH_UNEXPECTED_ERROR",
                    "Une erreur est survenue lors du changement de mot de passe.");
            }
        }

        public bool ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
                return false;

            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialChar = password.Any(character => !char.IsLetterOrDigit(character));

            return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }

        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();

        private static string NormalizeCurrency(string devise)
            => string.IsNullOrWhiteSpace(devise) ? string.Empty : devise.Trim().ToUpperInvariant();
    }
}
