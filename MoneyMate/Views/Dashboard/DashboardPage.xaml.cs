using MoneyMate.ViewModels.Dashboard;

namespace MoneyMate.Views.Dashboard
{
    public partial class DashboardPage : BasePage
    {
        private readonly DashboardViewModel _viewModel;
        private bool _hasAppearedOnce;

        public DashboardPage(DashboardViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            SetViewModel(_viewModel);
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_hasAppearedOnce)
            {
                _hasAppearedOnce = true;
                await _viewModel.EnsureLoadedAsync();
            }
        }
    }
}
