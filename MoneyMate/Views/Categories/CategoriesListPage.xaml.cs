using MoneyMate.ViewModels.Categories;

namespace MoneyMate.Views.Categories
{
    public partial class CategoriesListPage : BasePage
    {
        private CategoriesViewModel ViewModel => (CategoriesViewModel)BindingContext;

        public CategoriesListPage(CategoriesViewModel viewModel)
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
