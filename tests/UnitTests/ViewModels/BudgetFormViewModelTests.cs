using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels;
using MoneyMate.ViewModels.Budgets;

namespace UnitTests.ViewModels;

[TestClass]
public class BudgetFormViewModelTests
{
    [TestMethod]
    public async Task InitializeAsync_LoadsMonthOptionsAndDefaults()
    {
        User user = ViewModelTestHelper.CreateUser();

        BudgetFormViewModel viewModel = new(
            new Mock<IBudgetService>().Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.MonthOptions.Count > 0);
        Assert.IsNotNull(viewModel.SelectedMonth);
        Assert.AreEqual(DateTime.Today.Month, viewModel.SelectedMonth!.Month);
        Assert.IsTrue(viewModel.IsActive);
    }

    [TestMethod]
    public async Task SaveCommand_WhenMonthMissing_DoesNotCallService()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IBudgetService> budgetServiceMock = new();

        BudgetFormViewModel viewModel = new(
            budgetServiceMock.Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();
        viewModel.AmountText = "100";
        viewModel.SelectedMonth = null;

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        Assert.IsTrue(viewModel.HasValidationErrors);
        budgetServiceMock.Verify(x => x.CreateBudgetAsync(It.IsAny<Budget>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveCommand_WithDashboardReturnRoute_CreatesBudgetAndNavigatesToDashboard()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        budgetServiceMock.Setup(x => x.CreateBudgetAsync(It.IsAny<Budget>()))
            .ReturnsAsync((Budget budget) =>
            {
                budget.Id = 12;
                return ServiceResult<Budget>.Success(budget);
            });

        BudgetFormViewModel viewModel = new(
            budgetServiceMock.Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        await viewModel.InitializeAsync(new Dictionary<string, object>
        {
            [NavigationParameterKeys.ReturnRoute] = AppRoutes.Dashboard
        });

        viewModel.AmountText = "250";
        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        budgetServiceMock.Verify(
            x => x.CreateBudgetAsync(It.Is<Budget>(budget =>
                budget.UserId == user.Id &&
                budget.Amount == 250m &&
                budget.StartDate.Month == DateTime.Today.Month &&
                budget.StartDate.Year == DateTime.Today.Year)),
            Times.Once);

        navigationServiceMock.Verify(x => x.NavigateToAsync(AppRoutes.Dashboard), Times.Once);
    }

    [TestMethod]
    public async Task SaveCommand_WhenBudgetAlreadyExists_ShowsCleanErrorAndStaysOnForm()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        budgetServiceMock.Setup(x => x.CreateBudgetAsync(It.IsAny<Budget>()))
            .ReturnsAsync(ServiceResult<Budget>.Failure(
                "BUDGET_CONFLICT",
                "Un budget existe déjà pour ce mois."));

        BudgetFormViewModel viewModel = new(
            budgetServiceMock.Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        await viewModel.InitializeAsync();
        viewModel.AmountText = "250";

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        Assert.AreEqual("Un budget existe déjà pour ce mois.", viewModel.ErrorMessage);
        navigationServiceMock.Verify(x => x.NavigateToAsync(It.IsAny<string>()), Times.Never);
    }
}
