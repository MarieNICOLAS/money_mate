using MoneyMate.ViewModels.FixedCharges;

namespace MoneyMate.Views.FixedCharges
{
    public partial class EditFixedChargePage : BasePage, IQueryAttributable
    {
        private readonly FixedChargeFormViewModel _viewModel;
        private Dictionary<string, object>? _queryParameters;
        private bool _isInitialized;

        public EditFixedChargePage(FixedChargeFormViewModel viewModel)
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
