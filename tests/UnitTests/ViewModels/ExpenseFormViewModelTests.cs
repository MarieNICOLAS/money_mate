using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels;
using MoneyMate.ViewModels.Expenses;

namespace UnitTests.ViewModels;

[TestClass]
public class ExpenseFormViewModelTests
{
    [TestMethod]
    public async Task InitializeAsync_WithEditParameter_LoadsExpenseAndCategories()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success(new List<Budget>
            {
                new() { Id = 1, UserId = user.Id, Amount = 100m, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(27), IsActive = true }
            }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 3, Name = "Transport", IsActive = true, IsSystem = true }
            }));

        expenseServiceMock.Setup(x => x.GetExpenseByIdAsync(8, user.Id))
            .ReturnsAsync(ServiceResult<Expense>.Success(new Expense
            {
                Id = 8,
                UserId = user.Id,
                CategoryId = 3,
                Amount = 42.5m,
                DateOperation = DateTime.Today,
                Note = "Taxi",
                IsFixedCharge = false
            }));

        ExpenseFormViewModel viewModel = new(
            expenseServiceMock.Object,
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync(new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = 8
        });

        Assert.IsTrue(viewModel.IsEditMode);
        Assert.AreEqual(1, viewModel.Categories.Count);
        Assert.IsTrue(viewModel.AmountText is "42.5" or "42,5");
        Assert.AreEqual(3, viewModel.SelectedCategoryId);
        Assert.AreEqual("Taxi", viewModel.Note);
    }

    [TestMethod]
    public async Task SaveCommand_WhenValid_CreatesExpenseAndNavigates()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success(new List<Budget>
            {
                new() { Id = 1, UserId = user.Id, Amount = 100m, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(27), IsActive = true }
            }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 3, Name = "Transport", IsActive = true, IsSystem = true }
            }));

        expenseServiceMock.Setup(x => x.CreateExpenseAsync(It.IsAny<Expense>()))
            .ReturnsAsync(ServiceResult<Expense>.Success(new Expense { Id = 1, UserId = user.Id }));

        ExpenseFormViewModel viewModel = new(
            expenseServiceMock.Object,
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        await viewModel.InitializeAsync();
        viewModel.AmountText = "35.90";
        viewModel.SelectedCategoryId = 3;
        viewModel.Note = "Bus";

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        expenseServiceMock.Verify(x => x.CreateExpenseAsync(It.Is<Expense>(expense =>
            expense.UserId == user.Id &&
            expense.CategoryId == 3 &&
            expense.Amount == 35.90m)), Times.Once);
        navigationServiceMock.Verify(x => x.NavigateToAsync(AppRoutes.ExpensesList), Times.Once);
    }

    [TestMethod]
    public async Task SaveCommand_WhenDateIsInFuture_DoesNotCallService()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success(new List<Budget>
            {
                new() { Id = 1, UserId = user.Id, Amount = 100m, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(27), IsActive = true }
            }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 3, Name = "Transport", IsActive = true, IsSystem = true }
            }));

        ExpenseFormViewModel viewModel = new(
            expenseServiceMock.Object,
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();
        viewModel.AmountText = "20";
        viewModel.SelectedCategoryId = 3;
        viewModel.DateOperation = DateTime.Now.AddDays(1);

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        Assert.IsTrue(viewModel.HasValidationErrors);
        expenseServiceMock.Verify(x => x.CreateExpenseAsync(It.IsAny<Expense>()), Times.Never);
    }
}
