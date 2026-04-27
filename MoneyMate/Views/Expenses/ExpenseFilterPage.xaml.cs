using MoneyMate.ViewModels.Expenses;

namespace MoneyMate.Views.Expenses;

public partial class ExpenseFilterPage : BasePage
{
    private ExpenseFilterViewModel ViewModel => (ExpenseFilterViewModel)BindingContext;

    public ExpenseFilterPage(ExpenseFilterViewModel viewModel)
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
