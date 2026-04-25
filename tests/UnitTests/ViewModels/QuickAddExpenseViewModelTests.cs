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
}
