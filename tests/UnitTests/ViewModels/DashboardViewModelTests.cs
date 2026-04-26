using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Configuration;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels;
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
                CurrentMonthBudget = 500m,
                CurrentMonthBalance = 375m,
                HasCurrentMonthBudget = true,
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

        Assert.AreEqual("user", viewModel.UserName);
        Assert.AreEqual("Bonjour user", viewModel.GreetingText);
        Assert.IsFalse(string.IsNullOrWhiteSpace(viewModel.TodayDisplay));
        Assert.AreEqual("3", viewModel.ExpensesCountDisplay);
        Assert.AreEqual("2", viewModel.ActiveBudgetsDisplay);
        Assert.AreEqual("1", viewModel.TriggeredAlertsDisplay);
        Assert.IsTrue(viewModel.HasCurrentMonthBudget);
        Assert.IsFalse(viewModel.IsCurrentMonthBudgetMissing);
        Assert.IsTrue(viewModel.CurrentMonthExpensesDisplay.Contains("€"));
        Assert.IsTrue(viewModel.HasTopCategories);
        Assert.AreEqual(1, viewModel.TopCategories.Count);
        Assert.AreEqual("Courses", viewModel.TopCategories[0].CategoryName);
    }

    [TestMethod]
    public async Task LoadAsync_WithoutCurrentMonthBudget_ShowsBudgetEmptyState()
    {
        var user = ViewModelTestHelper.CreateUser();
        Mock<IDashboardService> dashboardServiceMock = new();

        dashboardServiceMock.Setup(x => x.GetDashboardSummaryAsync(user.Id))
            .ReturnsAsync(ServiceResult<DashboardSummary>.Success(new DashboardSummary
            {
                HasCurrentMonthBudget = false,
                CurrentMonthBudget = 0m
            }));

        DashboardViewModel viewModel = new(
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            dashboardServiceMock.Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        Assert.IsFalse(viewModel.IsCurrentMonthBudgetMissing);

        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.HasCurrentMonthBudget);
        Assert.IsTrue(viewModel.IsCurrentMonthBudgetMissing);
    }

    [TestMethod]
    public async Task CreateBudgetCommand_NavigatesToAddBudgetWithDashboardReturnRoute()
    {
        var user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        DashboardViewModel viewModel = new(
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            new Mock<IDashboardService>().Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        viewModel.CreateBudgetCommand.Execute(null);
        await Task.Delay(100);

        navigationServiceMock.Verify(
            x => x.NavigateToAsync(
                AppRoutes.AddBudget,
                It.Is<Dictionary<string, object>>(parameters =>
                    parameters.ContainsKey(NavigationParameterKeys.ReturnRoute) &&
                    (string)parameters[NavigationParameterKeys.ReturnRoute] == AppRoutes.Dashboard)),
            Times.Once);
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
        navigationServiceMock.Verify(x => x.NavigateToAsync(AppRoutes.Main), Times.Once);
    }

    [TestMethod]
    public async Task RefreshCommand_LoadsSummary()
    {
        var user = ViewModelTestHelper.CreateUser();
        Mock<IDashboardService> dashboardServiceMock = new();

        dashboardServiceMock.Setup(x => x.GetDashboardSummaryAsync(user.Id))
            .ReturnsAsync(ServiceResult<DashboardSummary>.Success(new DashboardSummary()));

        DashboardViewModel viewModel = new(
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            dashboardServiceMock.Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        viewModel.RefreshCommand.Execute(null);
        await Task.Delay(100);

        dashboardServiceMock.Verify(x => x.GetDashboardSummaryAsync(user.Id), Times.Once);
    }

    [TestMethod]
    public async Task LoadAsync_WithoutCurrentUser_SetsSessionError()
    {
        Mock<IDashboardService> dashboardServiceMock = new();

        DashboardViewModel viewModel = new(
            ViewModelTestHelper.CreateAuthenticationServiceMock(null).Object,
            dashboardServiceMock.Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual("Aucune session utilisateur active.", viewModel.ErrorMessage);
        Assert.IsFalse(viewModel.IsCurrentMonthBudgetMissing);
        dashboardServiceMock.Verify(x => x.GetDashboardSummaryAsync(It.IsAny<int>()), Times.Never);
    }
}
