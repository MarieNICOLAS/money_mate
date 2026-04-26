using MoneyMate.Infrastructure;
using MoneyMate.ViewModels.Stats;

namespace MoneyMate.Views.Stats;

public partial class StatsOverviewPage : BasePage
{
    private readonly StatsOverviewViewModel _viewModel;

    public StatsOverviewPage()
        : this(ServiceResolver.GetRequiredService<StatsOverviewViewModel>())
    {
    }

    public StatsOverviewPage(StatsOverviewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
