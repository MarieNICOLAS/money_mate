using MoneyMate.ViewModels.Categories;

namespace MoneyMate.Views.Categories
{
    public partial class AddCategoryPage : BasePage
    {
        private readonly CategoryFormViewModel _viewModel;
        private bool _isInitialized;

        public AddCategoryPage(CategoryFormViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            SetViewModel(_viewModel);

            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isInitialized)
                return;

            _isInitialized = true;
            await _viewModel.InitializeAsync();
        }
    }
}
