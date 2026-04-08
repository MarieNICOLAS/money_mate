using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels.Alerts;

namespace UnitTests.ViewModels;

[TestClass]
public class AlertThresholdFormViewModelTests
{
    [TestMethod]
    public async Task InitializeAsync_LoadsBudgetsAndCategories()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 10, Name = "Courses", IsActive = true, IsSystem = true }
            }));

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success(new List<Budget>
            {
                new() { Id = 3, UserId = user.Id, CategoryId = 10, Amount = 200m, PeriodType = "Monthly", StartDate = DateTime.Today }
            }));

        AlertThresholdFormViewModel viewModel = new(
            new Mock<IAlertThresholdService>().Object,
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();

        Assert.AreEqual(1, viewModel.Categories.Count);
        Assert.AreEqual(1, viewModel.Budgets.Count);
        Assert.AreEqual("Warning", viewModel.SelectedAlertType);
    }

    [TestMethod]
    public async Task SaveCommand_WhenBudgetCategoryMismatch_DoesNotCallService()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IAlertThresholdService> alertServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 10, Name = "Courses", IsActive = true, IsSystem = true },
                new() { Id = 20, Name = "Transport", IsActive = true, IsSystem = true }
            }));

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success(new List<Budget>
            {
                new() { Id = 3, UserId = user.Id, CategoryId = 10, Amount = 200m, PeriodType = "Monthly", StartDate = DateTime.Today }
            }));

        AlertThresholdFormViewModel viewModel = new(
            alertServiceMock.Object,
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();
        viewModel.SelectedBudgetId = 3;
        viewModel.SelectedCategoryId = 20;
        viewModel.ThresholdPercentageText = "80";

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        Assert.IsTrue(viewModel.HasValidationErrors);
        alertServiceMock.Verify(x => x.CreateAlertThresholdAsync(It.IsAny<AlertThreshold>()), Times.Never);
    }
}
