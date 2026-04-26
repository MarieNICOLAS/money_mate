using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels.Expenses;

namespace UnitTests.ViewModels;

[TestClass]
public class QuickAddExpenseViewModelTests
{
    [TestMethod]
    public async Task SaveCommand_WhenValid_CreatesExpense()
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
                new() { Id = 6, Name = "Courses", IsActive = true, IsSystem = true }
            }));

        expenseServiceMock.Setup(x => x.CreateExpenseAsync(It.IsAny<Expense>()))
            .ReturnsAsync(ServiceResult<Expense>.Success(new Expense { Id = 2, UserId = user.Id }));

        QuickAddExpenseViewModel viewModel = new(
            expenseServiceMock.Object,
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();
        viewModel.AmountText = "12.5";
        viewModel.SelectedCategoryId = 6;
        viewModel.Note = "Sandwich";

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        expenseServiceMock.Verify(x => x.CreateExpenseAsync(It.Is<Expense>(expense =>
            expense.Amount == 12.5m &&
            expense.CategoryId == 6 &&
            expense.Note == "Sandwich")), Times.Once);
    }

    [TestMethod]
    public async Task InitializeAsync_WhenNoBudget_ShowsBudgetRequirement()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success([]));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success([
                new() { Id = 6, Name = "Courses", IsActive = true, IsSystem = true }
            ]));

        QuickAddExpenseViewModel viewModel = CreateViewModel(
            user,
            expenseServiceMock,
            budgetServiceMock,
            categoryServiceMock);

        await viewModel.InitializeAsync();

        Assert.IsFalse(viewModel.HasAvailableBudget);
        Assert.IsTrue(viewModel.HasBudgetRequirementError);
        Assert.IsFalse(viewModel.CanSave);
    }

    [TestMethod]
    public async Task RefreshAsync_WhenBudgetWasCreated_HidesBudgetRequirement()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        budgetServiceMock.SetupSequence(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success([]))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success([
                new() { Id = 1, UserId = user.Id, Amount = 100m, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(27), IsActive = true }
            ]));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success([
                new() { Id = 6, Name = "Courses", IsActive = true, IsSystem = true }
            ]));

        QuickAddExpenseViewModel viewModel = CreateViewModel(
            user,
            expenseServiceMock,
            budgetServiceMock,
            categoryServiceMock);

        await viewModel.InitializeAsync();
        Assert.IsTrue(viewModel.HasBudgetRequirementError);

        await viewModel.RefreshAsync();

        Assert.IsTrue(viewModel.HasAvailableBudget);
        Assert.IsFalse(viewModel.HasBudgetRequirementError);
    }

    [TestMethod]
    public async Task SaveCommand_WhenAmountIsSetButNoBudget_BlocksExpenseCreation()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success([]));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success([
                new() { Id = 6, Name = "Courses", IsActive = true, IsSystem = true }
            ]));

        QuickAddExpenseViewModel viewModel = CreateViewModel(
            user,
            expenseServiceMock,
            budgetServiceMock,
            categoryServiceMock);

        await viewModel.InitializeAsync();
        viewModel.AmountText = "12.5";
        viewModel.SelectedCategoryId = 6;

        Assert.IsFalse(viewModel.SaveCommand.CanExecute(null));
        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        expenseServiceMock.Verify(x => x.CreateExpenseAsync(It.IsAny<Expense>()), Times.Never);
    }

    [TestMethod]
    public async Task CancelCommand_NavigatesBack()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success([
                new() { Id = 1, UserId = user.Id, Amount = 100m, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(27), IsActive = true }
            ]));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success([
                new() { Id = 6, Name = "Courses", IsActive = true, IsSystem = true }
            ]));

        QuickAddExpenseViewModel viewModel = CreateViewModel(
            user,
            expenseServiceMock,
            budgetServiceMock,
            categoryServiceMock,
            navigationServiceMock);

        viewModel.CancelCommand.Execute(null);
        await Task.Delay(100);

        navigationServiceMock.Verify(x => x.GoBackAsync(), Times.Once);
    }

    private static QuickAddExpenseViewModel CreateViewModel(
        User user,
        Mock<IExpenseService> expenseServiceMock,
        Mock<IBudgetService> budgetServiceMock,
        Mock<ICategoryService> categoryServiceMock,
        Mock<INavigationService>? navigationServiceMock = null)
    {
        return new QuickAddExpenseViewModel(
            expenseServiceMock.Object,
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            (navigationServiceMock ?? ViewModelTestHelper.CreateNavigationServiceMock()).Object);
    }
}
