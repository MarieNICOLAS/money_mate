using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;

namespace UnitTests.ViewModels;

internal static class ViewModelTestHelper
{
    public static User CreateUser(int id = 1, string devise = "EUR")
        => new()
        {
            Id = id,
            Email = "user@test.com",
            Devise = devise,
            Role = "User",
            IsActive = true
        };

    public static Mock<IAuthenticationService> CreateAuthenticationServiceMock(User? user = null)
    {
        Mock<IAuthenticationService> authenticationServiceMock = new();

        authenticationServiceMock.Setup(x => x.GetCurrentUser())
            .Returns(user);

        authenticationServiceMock.SetupGet(x => x.IsAuthenticated)
            .Returns(user != null);

        authenticationServiceMock.Setup(x => x.LogoutAsync(It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        return authenticationServiceMock;
    }

    public static Mock<IDialogService> CreateDialogServiceMock(bool confirmationResult = true)
    {
        Mock<IDialogService> dialogServiceMock = new();

        dialogServiceMock.Setup(x => x.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        dialogServiceMock.Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(confirmationResult);

        return dialogServiceMock;
    }

    public static Mock<INavigationService> CreateNavigationServiceMock()
    {
        Mock<INavigationService> navigationServiceMock = new();

        navigationServiceMock.Setup(x => x.NavigateToAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        navigationServiceMock.Setup(x => x.NavigateToAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(Task.CompletedTask);

        navigationServiceMock.Setup(x => x.GoBackAsync())
            .Returns(Task.CompletedTask);

        navigationServiceMock.Setup(x => x.NavigateToMainAsync())
            .Returns(Task.CompletedTask);

        navigationServiceMock.Setup(x => x.PresentModalAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        navigationServiceMock.Setup(x => x.DismissModalAsync())
            .Returns(Task.CompletedTask);

        return navigationServiceMock;
    }
}
