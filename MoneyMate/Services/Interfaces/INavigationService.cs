namespace MoneyMate.Services.Interfaces
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route);

        Task NavigateToAsync(string route, Dictionary<string, object> parameters);

        Task GoBackAsync();

        Task NavigateToMainAsync();

        Task PresentModalAsync(string route);

        Task DismissModalAsync();
    }
}
