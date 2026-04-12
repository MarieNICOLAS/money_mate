using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels;
using MoneyMate.ViewModels.Budgets;

namespace UnitTests.ViewModels;

[TestClass]
public class BudgetsOverviewViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_WithBudgets_ComputesTotalsAndRiskCount()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        Budget budget1 = new() { Id = 1, UserId = user.Id, Amount = 100m, PeriodType = "Monthly", StartDate = new DateTime(2026, 4, 1) };
        Budget budget2 = new() { Id = 2, UserId = user.Id, Amount = 200m, PeriodType = "Monthly", StartDate = new DateTime(2026, 3, 1) };

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success(new List<Budget> { budget1, budget2 }));

        budgetServiceMock.Setup(x => x.GetBudgetConsumptionSummaryAsync(1, user.Id))
            .ReturnsAsync(ServiceResult<BudgetConsumptionSummary>.Success(new BudgetConsumptionSummary
            {
                Budget = budget1,
                ConsumedAmount = 90m,
                RemainingAmount = 10m,
                ConsumedPercentage = 90m,
                IsExceeded = false
            }));

        budgetServiceMock.Setup(x => x.GetBudgetConsumptionSummaryAsync(2, user.Id))
            .ReturnsAsync(ServiceResult<BudgetConsumptionSummary>.Success(new BudgetConsumptionSummary
            {
                Budget = budget2,
                ConsumedAmount = 50m,
                RemainingAmount = 150m,
                ConsumedPercentage = 25m,
                IsExceeded = false
            }));

        BudgetsOverviewViewModel viewModel = new(
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.Budgets.Count);
        Assert.AreEqual(300m, viewModel.TotalBudgetAmount);
        Assert.AreEqual(140m, viewModel.TotalConsumedAmount);
        Assert.AreEqual(2, viewModel.ActiveBudgetsCount);
        Assert.AreEqual(1, viewModel.BudgetsAtRiskCount);
        Assert.IsTrue(viewModel.HasBudgets);
        Assert.AreEqual("avril 2026", viewModel.Budgets[0].PeriodLabel.ToLowerInvariant());
    }

    [TestMethod]
    public async Task AddBudgetCommand_NavigatesToAddBudgetPage()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        BudgetsOverviewViewModel viewModel = new(
            new Mock<IBudgetService>().Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        viewModel.AddBudgetCommand.Execute(null);
        await Task.Delay(100);

        navigationServiceMock.Verify(x => x.NavigateToAsync("AddBudgetPage"), Times.Once);
    }

    [TestMethod]
    public async Task ManageCategoriesCommand_NavigatesToCategoriesListPage()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        BudgetsOverviewViewModel viewModel = new(
            new Mock<IBudgetService>().Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        viewModel.ManageCategoriesCommand.Execute(null);
        await Task.Delay(100);

        navigationServiceMock.Verify(x => x.NavigateToAsync("//CategoriesListPage"), Times.Once);
    }

    [TestMethod]
    public async Task OpenEditBudgetCommand_NavigatesWithBudgetId()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        BudgetsOverviewViewModel viewModel = new(
            new Mock<IBudgetService>().Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        viewModel.OpenEditBudgetCommand.Execute(new BudgetOverviewItemViewModel { Id = 42, PeriodLabel = "avril 2026" });
        await Task.Delay(100);

        navigationServiceMock.Verify(
            x => x.NavigateToAsync($"EditBudgetPage?{NavigationParameterKeys.BudgetId}=42"),
            Times.Once);
    }

    [TestMethod]
    public async Task LoadAsync_WithoutCurrentUser_SetsSessionError()
    {
        Mock<IBudgetService> budgetServiceMock = new();

        BudgetsOverviewViewModel viewModel = new(
            budgetServiceMock.Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(null).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual("Aucune session utilisateur active.", viewModel.ErrorMessage);
        budgetServiceMock.Verify(x => x.GetBudgetsAsync(It.IsAny<int>()), Times.Never);
    }
}
