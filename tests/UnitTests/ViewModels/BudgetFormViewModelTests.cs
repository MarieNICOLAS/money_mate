using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
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
}
