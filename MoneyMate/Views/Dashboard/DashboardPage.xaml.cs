using MoneyMate.ViewModels.Dashboard;

namespace MoneyMate.Views.Dashboard
{
    public partial class DashboardPage : BasePage
    {
        private DashboardViewModel ViewModel => (DashboardViewModel)BindingContext;

        public DashboardPage(DashboardViewModel viewModel)
        {
            SetViewModel(viewModel);
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.LoadUser();
        }
    }
}
