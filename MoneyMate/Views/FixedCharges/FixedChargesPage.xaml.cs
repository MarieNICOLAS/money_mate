using MoneyMate.ViewModels.FixedCharges;

namespace MoneyMate.Views.FixedCharges
{
    public partial class FixedChargesPage : BasePage
    {
        private FixedChargesViewModel ViewModel => (FixedChargesViewModel)BindingContext;

        public FixedChargesPage(FixedChargesViewModel viewModel)
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
