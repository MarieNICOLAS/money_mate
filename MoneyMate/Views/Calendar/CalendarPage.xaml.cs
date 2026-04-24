using MoneyMate.ViewModels.Calendar;
using MoneyMate.Views;

namespace MoneyMate.Views.Calendar
{
    public partial class CalendarPage : BasePage
    {
        private readonly CalendarViewModel _viewModel;

        public CalendarPage(CalendarViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            SetViewModel(_viewModel);
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
}
