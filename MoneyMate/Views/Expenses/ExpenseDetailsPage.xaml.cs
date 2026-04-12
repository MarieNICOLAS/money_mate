using MoneyMate.ViewModels.Expenses;

namespace MoneyMate.Views.Expenses
{
    public partial class ExpenseDetailsPage : BasePage, IQueryAttributable
    {
        private readonly ExpenseDetailsViewModel _viewModel;
        private Dictionary<string, object>? _queryParameters;
        private bool _isInitialized;

        public ExpenseDetailsPage(ExpenseDetailsViewModel viewModel)
        {
            _viewModel = viewModel;
            SetViewModel(viewModel);
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

            if (!_isInitialized)
            {
                _isInitialized = true;
                await _viewModel.InitializeAsync(_queryParameters);
                return;
            }

            await _viewModel.LoadAsync();
        }
    }
}
