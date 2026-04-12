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
    public async Task InitializeAsync_LoadsCategoriesAndDefaults()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 10, Name = "Courses", IsActive = true, IsSystem = true }
            }));

        BudgetFormViewModel viewModel = new(
            new Mock<IBudgetService>().Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();

        Assert.AreEqual(1, viewModel.Categories.Count);
        Assert.AreEqual(10, viewModel.SelectedCategoryId);
        Assert.AreEqual("Monthly", viewModel.SelectedPeriodType);
        Assert.IsFalse(viewModel.HasEndDate);
    }

    [TestMethod]
    public async Task SaveCommand_WhenPeriodInvalid_DoesNotCallService()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 10, Name = "Courses", IsActive = true, IsSystem = true }
            }));

        BudgetFormViewModel viewModel = new(
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();
        viewModel.AmountText = "100";
        viewModel.SelectedCategoryId = 10;
        viewModel.HasEndDate = true;
        viewModel.StartDate = DateTime.Today;
        viewModel.EndDate = DateTime.Today.AddDays(-1);

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        Assert.IsTrue(viewModel.HasValidationErrors);
        budgetServiceMock.Verify(x => x.CreateBudgetAsync(It.IsAny<Budget>()), Times.Never);
    }
}
