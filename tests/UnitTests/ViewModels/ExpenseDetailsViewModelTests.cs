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
public class ExpenseDetailsViewModelTests
{
    [TestMethod]
    public async Task InitializeAsync_WithExpenseId_LoadsExpenseDetails()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        expenseServiceMock.Setup(x => x.GetExpenseByIdAsync(14, user.Id))
            .ReturnsAsync(ServiceResult<Expense>.Success(new Expense
            {
                Id = 14,
                UserId = user.Id,
                CategoryId = 2,
                Amount = 18.75m,
                Note = "Déjeuner",
                DateOperation = DateTime.Today,
                IsFixedCharge = false
            }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 2, Name = "Repas", Color = "#4CAF50", Icon = "🍽️", IsActive = true, IsSystem = true }
            }));

        ExpenseDetailsViewModel viewModel = new(
            expenseServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync(new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = 14
        });

        Assert.AreEqual(14, viewModel.ExpenseId);
        Assert.AreEqual("Repas", viewModel.CategoryName);
        Assert.AreEqual("Déjeuner", viewModel.Note);
        Assert.IsTrue(viewModel.AmountDisplay.Contains("€"));
        Assert.IsTrue(viewModel.HasExpense);
    }

    [TestMethod]
    public async Task EditCommand_NavigatesWithExpenseId()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        expenseServiceMock.Setup(x => x.GetExpenseByIdAsync(14, user.Id))
            .ReturnsAsync(ServiceResult<Expense>.Success(new Expense
            {
                Id = 14,
                UserId = user.Id,
                CategoryId = 2,
                Amount = 18.75m,
                Note = "Déjeuner",
                DateOperation = DateTime.Today
            }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>()));

        ExpenseDetailsViewModel viewModel = new(
            expenseServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        await viewModel.InitializeAsync(new Dictionary<string, object>
        {
            [NavigationParameterKeys.ExpenseId] = 14
        });

        viewModel.EditCommand.Execute(null);
        await Task.Delay(100);

        navigationServiceMock.Verify(x => x.NavigateToAsync(AppRoutes.EditExpense, It.Is<Dictionary<string, object>>(parameters =>
            parameters.ContainsKey(NavigationParameterKeys.ExpenseId) &&
            (int)parameters[NavigationParameterKeys.ExpenseId] == 14)), Times.Once);
    }
}
