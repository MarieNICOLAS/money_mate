using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels.FixedCharges;

namespace UnitTests.ViewModels;

[TestClass]
public class FixedChargesViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_WithFixedCharges_ComputesProjectionAndUpcomingCount()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IFixedChargeService> fixedChargeServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        fixedChargeServiceMock.Setup(x => x.GetFixedChargesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<FixedCharge>>.Success(new List<FixedCharge>
            {
                new()
                {
                    Id = 1,
                    UserId = user.Id,
                    Name = "Netflix",
                    Amount = 30m,
                    CategoryId = 10,
                    Frequency = "Monthly",
                    DayOfMonth = DateTime.Now.Day,
                    StartDate = DateTime.Now.AddMonths(-2),
                    IsActive = true,
                    AutoCreateExpense = true
                },
                new()
                {
                    Id = 2,
                    UserId = user.Id,
                    Name = "Assurance",
                    Amount = 120m,
                    CategoryId = 10,
                    Frequency = "Yearly",
                    DayOfMonth = DateTime.Now.Day,
                    StartDate = DateTime.Now.AddYears(-1),
                    IsActive = true,
                    AutoCreateExpense = false
                }
            }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 10, Name = "Abonnements", Color = "#2196F3", Icon = "📺", IsActive = true }
            }));

        FixedChargesViewModel viewModel = new(
            fixedChargeServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.FixedCharges.Count);
        Assert.AreEqual(40m, viewModel.ProjectedMonthlyAmount);
        Assert.AreEqual(2, viewModel.ActiveFixedChargesCount);
        Assert.AreEqual(1, viewModel.UpcomingFixedChargesCount);
        Assert.IsTrue(viewModel.HasFixedCharges);
        Assert.IsTrue(viewModel.FixedCharges.Any(charge => charge.Name == "Netflix" && charge.FrequencyLabel == "Mensuelle"));
        Assert.IsTrue(viewModel.FixedCharges.Any(charge => charge.Name == "Assurance" && charge.FrequencyLabel == "Annuelle"));
    }

    [TestMethod]
    public async Task GenerateExpensesCommand_ShowsGeneratedCount()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IFixedChargeService> fixedChargeServiceMock = new();
        Mock<IDialogService> dialogServiceMock = ViewModelTestHelper.CreateDialogServiceMock();

        fixedChargeServiceMock.Setup(x => x.GenerateExpensesUntilAsync(user.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(ServiceResult<List<Expense>>.Success(new List<Expense>
            {
                new() { Id = 1, UserId = user.Id, Amount = 20m, CategoryId = 10, DateOperation = DateTime.Today }
            }));

        fixedChargeServiceMock.Setup(x => x.GetFixedChargesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<FixedCharge>>.Success(new List<FixedCharge>()));

        Mock<ICategoryService> categoryServiceMock = new();
        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>()));

        FixedChargesViewModel viewModel = new(
            fixedChargeServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            dialogServiceMock.Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        viewModel.GenerateExpensesCommand.Execute(null);
        await Task.Delay(100);

        fixedChargeServiceMock.Verify(x => x.GenerateExpensesUntilAsync(user.Id, It.IsAny<DateTime>()), Times.Once);
        dialogServiceMock.Verify(x => x.ShowAlertAsync("Charges fixes", It.Is<string>(message => message.Contains("1 dépense(s) récurrente(s) générée(s).")), "OK"), Times.Once);
    }

    [TestMethod]
    public async Task LoadAsync_WithoutCurrentUser_SetsSessionError()
    {
        Mock<IFixedChargeService> fixedChargeServiceMock = new();

        FixedChargesViewModel viewModel = new(
            fixedChargeServiceMock.Object,
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(null).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual("Aucune session utilisateur active.", viewModel.ErrorMessage);
        fixedChargeServiceMock.Verify(x => x.GetFixedChargesAsync(It.IsAny<int>()), Times.Never);
    }
}
