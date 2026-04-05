using MoneyMate.ViewModels.Profile;

namespace MoneyMate.Views.Profile
{
    public partial class ProfilePage : BasePage
    {
        private ProfileViewModel ViewModel => (ProfileViewModel)BindingContext;

        public ProfilePage(ProfileViewModel viewModel)
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
