using MoneyMate.ViewModels.Expenses;

namespace MoneyMate.Views.Expenses
{
    public partial class QuickAddExpensePage : BasePage
    {
        private readonly QuickAddExpenseViewModel _viewModel;
        private bool _isInitialized;

        public QuickAddExpensePage(QuickAddExpenseViewModel viewModel)
        {
            _viewModel = viewModel;
            SetViewModel(viewModel);
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
