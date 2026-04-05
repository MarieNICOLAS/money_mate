using MoneyMate.ViewModels.Profile;

namespace MoneyMate.Views.Profile
{
    public partial class DeleteAccountPage : BasePage
    {
        private DeleteAccountViewModel ViewModel => (DeleteAccountViewModel)BindingContext;

        public DeleteAccountPage(DeleteAccountViewModel viewModel)
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
