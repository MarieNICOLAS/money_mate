using MoneyMate.Models;

namespace MoneyMate.Services.Interfaces
{
    public interface ISessionManager
    {
        event EventHandler? SessionChanged;

        User? CurrentUser { get; }

        bool IsAuthenticated { get; }

        Task<bool> RestoreSessionAsync(CancellationToken cancellationToken = default);

        void StartSession(User user, bool rememberSession);

        void UpdateCurrentUser(User user);

        void ClearSession(bool clearPersistentSession = true);

        string GetRememberedEmail();

        bool GetRememberMePreference();

        bool HasRole(params string[] roles);

        bool CanAccessRoute(string route);
    }
}
