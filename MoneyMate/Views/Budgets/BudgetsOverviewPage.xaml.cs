using MoneyMate.ViewModels.Budgets;

namespace MoneyMate.Views.Budgets
{
    public partial class BudgetsOverviewPage : BasePage
    {
        private BudgetsOverviewViewModel ViewModel => (BudgetsOverviewViewModel)BindingContext;

        public BudgetsOverviewPage(BudgetsOverviewViewModel viewModel)
        {
            SetViewModel(viewModel);
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.RefreshIfNeededAsync();
        }
    }
}
