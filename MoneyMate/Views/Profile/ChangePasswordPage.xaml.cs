using MoneyMate.ViewModels.Profile;

namespace MoneyMate.Views.Profile
{
    public partial class ChangePasswordPage : BasePage
    {
        private ChangePasswordViewModel ViewModel => (ChangePasswordViewModel)BindingContext;

        public ChangePasswordPage(ChangePasswordViewModel viewModel)
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
