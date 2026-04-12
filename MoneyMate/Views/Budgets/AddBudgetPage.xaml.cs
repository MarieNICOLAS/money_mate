using MoneyMate.ViewModels.Budgets;

namespace MoneyMate.Views.Budgets
{
    public partial class AddBudgetPage : BasePage, IQueryAttributable
    {
        private readonly BudgetFormViewModel _viewModel;
        private Dictionary<string, object>? _queryParameters;
        private bool _isInitialized;

        public AddBudgetPage(BudgetFormViewModel viewModel)
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

            if (_isInitialized)
                return;

            _isInitialized = true;
            await _viewModel.InitializeAsync(_queryParameters);
        }
    }
}
