using MoneyMate.Models;
using MoneyMate.Services.Results;

namespace MoneyMate.Services.Interfaces
{
    public interface IAuthenticationService
    {
        event EventHandler? AuthenticationStateChanged;

        Task<ServiceResult<User>> LoginAsync(
            string email,
            string password,
            bool rememberSession = false);

        Task<ServiceResult<User>> RegisterAsync(
            string email,
            string password,
            string devise = "EUR");

        Task LogoutAsync(bool clearPersistentSession = true);

        User? GetCurrentUser();

        bool IsAuthenticated { get; }

        Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default);

        string GetRememberedEmail();

        bool GetRememberMePreference();

        bool HasRole(params string[] roles);

        bool CanAccessRoute(string route);

        Task<ServiceResult> ChangePasswordAsync(
            int userId,
            string oldPassword,
            string newPassword);

        bool ValidatePasswordStrength(string password);
    }
}
