using MoneyMate.ViewModels.Expenses;

namespace MoneyMate.Views.Expenses
{
    public partial class ExpensesListPage : BasePage
    {
        private ExpensesListViewModel ViewModel => (ExpensesListViewModel)BindingContext;

        public ExpensesListPage(ExpensesListViewModel viewModel)
        {
            SetViewModel(viewModel);
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.RefreshIfNeededAsync();
        }
    }
}
