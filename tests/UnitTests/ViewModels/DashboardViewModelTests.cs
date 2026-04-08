using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels.Dashboard;

namespace UnitTests.ViewModels;

[TestClass]
public class DashboardViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_WithSummary_PopulatesDisplayValuesAndTopCategories()
    {
        var user = ViewModelTestHelper.CreateUser(devise: "EUR");
        Mock<IDashboardService> dashboardServiceMock = new();

        dashboardServiceMock.Setup(x => x.GetDashboardSummaryAsync(user.Id))
            .ReturnsAsync(ServiceResult<DashboardSummary>.Success(new DashboardSummary
            {
                CurrentMonthExpenses = 125m,
                CurrentMonthExpensesCount = 3,
                ActiveBudgetsCount = 2,
                ActiveFixedChargesCount = 4,
                ActiveAlertsCount = 1,
                TriggeredAlertsCount = 1,
                PreviousMonthExpenses = 100m,
                ExpensesDeltaFromPreviousMonth = 25m,
                BudgetsAtRiskCount = 1,
                TopCategories = new List<DashboardCategorySpending>
                {
                    new() { CategoryId = 10, CategoryName = "Courses", TotalAmount = 80m, ExpensesCount = 2 }
                }
            }));

        DashboardViewModel viewModel = new(
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            dashboardServiceMock.Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual("user@test.com", viewModel.UserName);
        Assert.AreEqual("3", viewModel.ExpensesCountDisplay);
        Assert.AreEqual("2", viewModel.ActiveBudgetsDisplay);
        Assert.AreEqual("1", viewModel.TriggeredAlertsDisplay);
        Assert.IsTrue(viewModel.CurrentMonthExpensesDisplay.Contains("€"));
        Assert.IsTrue(viewModel.HasTopCategories);
        Assert.AreEqual(1, viewModel.TopCategories.Count);
        Assert.AreEqual("Courses", viewModel.TopCategories[0].CategoryName);
    }

    [TestMethod]
    public async Task LogoutCommand_WhenConfirmed_LogsOutAndNavigatesToMainPage()
    {
        var user = ViewModelTestHelper.CreateUser();
        Mock<IAuthenticationService> authenticationServiceMock = ViewModelTestHelper.CreateAuthenticationServiceMock(user);
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        DashboardViewModel viewModel = new(
            authenticationServiceMock.Object,
            new Mock<IDashboardService>().Object,
            ViewModelTestHelper.CreateDialogServiceMock(confirmationResult: true).Object,
            navigationServiceMock.Object);

        viewModel.LogoutCommand.Execute(null);
        await Task.Delay(100);

        authenticationServiceMock.Verify(x => x.LogoutAsync(true), Times.Once);
        navigationServiceMock.Verify(x => x.NavigateToAsync("//MainPage"), Times.Once);
    }
}
