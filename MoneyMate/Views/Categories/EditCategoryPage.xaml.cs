using MoneyMate.ViewModels.Categories;

namespace MoneyMate.Views.Categories
{
    public partial class EditCategoryPage : BasePage, IQueryAttributable
    {
        private readonly CategoryFormViewModel _viewModel;
        private Dictionary<string, object>? _queryParameters;
        private bool _isInitialized;

        public EditCategoryPage(CategoryFormViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            SetViewModel(_viewModel);

            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _queryParameters = query.ToDictionary(item => item.Key, item => item.Value);
            _isInitialized = false;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isInitialized)
                return;

            _isInitialized = true;
            await _viewModel.InitializeAsync(_queryParameters);
        }
    }
}
