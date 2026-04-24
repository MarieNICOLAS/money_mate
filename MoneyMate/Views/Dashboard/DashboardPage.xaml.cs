namespace MoneyMate.Views.Dashboard;

using MoneyMate.Infrastructure;
using MoneyMate.ViewModels.Dashboard;

public partial class DashboardPage : BasePage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage()
        : this(ServiceResolver.GetRequiredService<DashboardViewModel>())
    {
    }

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 🔥 CHARGEMENT APRES AFFICHAGE → ZERO FREEZE
        await _viewModel.InitializeAsync();
    }
}
