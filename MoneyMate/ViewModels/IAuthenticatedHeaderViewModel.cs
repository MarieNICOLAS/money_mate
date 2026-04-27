using System.Windows.Input;

namespace MoneyMate.ViewModels;

public interface IAuthenticatedHeaderViewModel
{
    ICommand NavigateToAlertsCommand { get; }

    ICommand NavigateToProfileCommand { get; }

    ICommand LogoutCommand { get; }

    bool HasNotificationBadge { get; }
}
