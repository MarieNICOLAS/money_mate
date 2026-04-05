using MoneyMate.Data.Context;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using System.Text.RegularExpressions;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Service d'authentification avec hashage BCrypt
    /// Conforme aux exigences de securite CONTRIBUTING.md
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private const int MIN_PASSWORD_LENGTH = 8;
        private readonly MoneyMateDbContext _dbContext;
        private User? _currentUser;

        public AuthenticationService()
        {
            _dbContext = DatabaseService.Instance;
        }

        public bool IsAuthenticated => _currentUser != null;

        /// <summary>
        /// Authentifie un utilisateur avec email et mot de passe
        /// </summary>
        public async Task<User?> LoginAsync(string email, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                        return null;

                    var user = _dbContext.GetUserByEmail(email.Trim().ToLower());

                    if (user == null || !user.IsActive)
                        return null;

                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                    if (!isPasswordValid)
                        return null;

                    _currentUser = user;
                    return user;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Erreur Login : {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Enregistre un nouvel utilisateur
        /// Hash du mot de passe avec BCrypt (facteur de travail 12)
        /// </summary>
        public async Task<User?> RegisterAsync(string email, string password, string devise = "EUR")
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                        return null;

                    email = email.Trim().ToLower();

                    if (!ValidatePasswordStrength(password))
                        return null;

                    if (_dbContext.GetUserByEmail(email) != null)
                        return null;

                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

                    var newUser = new User
                    {
                        Email = email,
                        PasswordHash = passwordHash,
                        Devise = devise,
                        BudgetStartDay = 1,
                        Role = "User",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    int userId = _dbContext.InsertUser(newUser);
                    newUser.Id = userId;

                    System.Diagnostics.Debug.WriteLine($"? Utilisateur cree : {email} (ID: {userId})");

                    return newUser;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Erreur Register : {ex.Message}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Deconnecte l utilisateur actuel
        /// </summary>
        public async Task LogoutAsync()
        {
            await Task.Run(() =>
            {
                _currentUser = null;
                System.Diagnostics.Debug.WriteLine("Utilisateur deconnecte");
            });
        }

        public User? GetCurrentUser() => _currentUser;

        /// <summary>
        /// Change le mot de passe de l'utilisateur
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var user = _dbContext.GetUserById(userId);
                    if (user == null)
                        return false;

                    if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
                        return false;

                    if (!ValidatePasswordStrength(newPassword))
                        return false;

                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

                    _dbContext.UpdateUser(user);

                    System.Diagnostics.Debug.WriteLine($"Mot de passe change pour l utilisateur {userId}");
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur ChangePassword : {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Valide la force d'un mot de passe
        /// Exigences : Min 8 car, 1 maj, 1 min, 1 chiffre, 1 special
        /// </summary>
        public bool ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MIN_PASSWORD_LENGTH)
                return false;

            var hasUpperCase = new Regex(@"[A-Z]");
            var hasLowerCase = new Regex(@"[a-z]");
            var hasDigit = new Regex(@"\d");
            var hasSpecialChar = new Regex(@"[!@#$%^&*(),.?""':;{}|<>]");

            return hasUpperCase.IsMatch(password)
                && hasLowerCase.IsMatch(password)
                && hasDigit.IsMatch(password)
                && hasSpecialChar.IsMatch(password);
        }
    }
}