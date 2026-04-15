using MoneyMate.Services.Interfaces;

namespace MoneyMate.Services.Implementations
{
    /// <summary>
    /// Implémentation MAUI du service de dialogue.
    /// Garantit l'exécution sur le thread principal et une résolution fiable de la page active.
    /// </summary>
    public class DialogService : IDialogService
    {
        public async Task ShowAlertAsync(string title, string message, string cancel)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Page? page = ResolveCurrentPage();
                if (page is null)
                    return;

                await page.DisplayAlert(title, message, cancel);
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Page? page = ResolveCurrentPage();
                if (page is null)
                    return false;

                return await page.DisplayAlert(title, message, accept, cancel);
            });
        }

        private static Page? ResolveCurrentPage()
        {
            if (Shell.Current?.CurrentPage is Page shellCurrentPage)
                return shellCurrentPage;

            Window? activeWindow = Application.Current?
                .Windows
                .FirstOrDefault(window => window.Page is not null);

            if (activeWindow?.Page is null)
                return null;

            return ResolveVisiblePage(activeWindow.Page);
        }

        private static Page? ResolveVisiblePage(Page page)
        {
            return page switch
            {
                NavigationPage navigationPage => navigationPage.CurrentPage is null
                    ? navigationPage
                    : ResolveVisiblePage(navigationPage.CurrentPage),

                FlyoutPage flyoutPage => flyoutPage.Detail is null
                    ? flyoutPage
                    : ResolveVisiblePage(flyoutPage.Detail),

                TabbedPage tabbedPage => tabbedPage.CurrentPage is null
                    ? tabbedPage
                    : ResolveVisiblePage(tabbedPage.CurrentPage),

                Shell shell when shell.CurrentPage is not null => shell.CurrentPage,

                _ => page
            };
        }
    }
}
