using MoneyMate.ViewModels.Alerts;

namespace MoneyMate.Views.Alerts
{
    public partial class AlertThresholdPage : BasePage
    {
        private AlertThresholdsViewModel ViewModel => (AlertThresholdsViewModel)BindingContext;

        public AlertThresholdPage(AlertThresholdsViewModel viewModel)
        {
            SetViewModel(viewModel);
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.LoadAsync();
        }
    }
}
