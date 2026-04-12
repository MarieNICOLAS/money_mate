using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels;
using MoneyMate.ViewModels.Expenses;

namespace UnitTests.ViewModels;

[TestClass]
public class ExpensesListViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_WithExpenses_ComputesTotalsAndMapsCategories()
    {
        User user = ViewModelTestHelper.CreateUser(devise: "EUR");
        Mock<IExpenseService> expenseServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        expenseServiceMock.Setup(x => x.GetExpensesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Expense>>.Success(new List<Expense>
            {
                new() { Id = 1, UserId = user.Id, CategoryId = 10, Amount = 12.50m, Note = "Déjeuner", DateOperation = DateTime.Today },
                new() { Id = 2, UserId = user.Id, CategoryId = 11, Amount = 27.50m, Note = string.Empty, IsFixedCharge = true, DateOperation = DateTime.Today.AddDays(-1) }
            }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 10, Name = "Repas", Color = "#4CAF50", Icon = "🍽️", IsActive = true, IsSystem = true },
                new() { Id = 11, Name = "Abonnements", Color = "#2196F3", Icon = "📺", IsActive = true, IsSystem = false, UserId = user.Id }
            }));

        ExpensesListViewModel viewModel = new(
            expenseServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.Expenses.Count);
        Assert.AreEqual(40m, viewModel.TotalExpenses);
        Assert.AreEqual(2, viewModel.ExpensesCount);
        Assert.IsTrue(viewModel.HasExpenses);
        Assert.AreEqual("Repas", viewModel.Expenses[0].CategoryName);
        Assert.AreEqual("EUR", viewModel.Expenses[0].Devise);
        Assert.AreEqual("Charge fixe", viewModel.Expenses[1].Note);
    }

    [TestMethod]
    public async Task AddExpenseCommand_NavigatesToAddExpensePage()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        ExpensesListViewModel viewModel = new(
            new Mock<IExpenseService>().Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        viewModel.AddExpenseCommand.Execute(null);
        await Task.Delay(100);

        navigationServiceMock.Verify(x => x.NavigateToAsync("//AddExpensePage"), Times.Once);
    }

    [TestMethod]
    public async Task QuickAddExpenseCommand_NavigatesToQuickAddExpensePage()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        ExpensesListViewModel viewModel = new(
            new Mock<IExpenseService>().Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        viewModel.QuickAddExpenseCommand.Execute(null);
        await Task.Delay(100);

        navigationServiceMock.Verify(x => x.NavigateToAsync("//QuickAddExpensePage"), Times.Once);
    }

    [TestMethod]
    public async Task OpenExpenseDetailsCommand_NavigatesWithExpenseId()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        ExpensesListViewModel viewModel = new(
            new Mock<IExpenseService>().Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        viewModel.OpenExpenseDetailsCommand.Execute(new ExpenseItemViewModel
        {
            Id = 42,
            Amount = 18.50m,
            CategoryName = "Repas",
            Devise = "EUR"
        });
        await Task.Delay(100);

        navigationServiceMock.Verify(x => x.NavigateToAsync("//ExpenseDetailsPage", It.Is<Dictionary<string, object>>(parameters =>
            parameters.ContainsKey(NavigationParameterKeys.ExpenseId) &&
            (int)parameters[NavigationParameterKeys.ExpenseId] == 42)), Times.Once);
    }

    [TestMethod]
    public async Task LoadAsync_WithoutCurrentUser_SetsSessionError()
    {
        Mock<IExpenseService> expenseServiceMock = new();

        ExpensesListViewModel viewModel = new(
            expenseServiceMock.Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(null).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual("Aucune session utilisateur active.", viewModel.ErrorMessage);
        expenseServiceMock.Verify(x => x.GetExpensesAsync(It.IsAny<int>()), Times.Never);
    }
}
