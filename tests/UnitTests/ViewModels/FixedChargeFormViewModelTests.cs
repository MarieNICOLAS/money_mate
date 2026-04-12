using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels;
using MoneyMate.ViewModels.FixedCharges;

namespace UnitTests.ViewModels;

[TestClass]
public class FixedChargeFormViewModelTests
{
    [TestMethod]
    public async Task InitializeAsync_WithEditParameter_LoadsFixedCharge()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IFixedChargeService> fixedChargeServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 4, Name = "Abonnements", IsActive = true, IsSystem = true }
            }));

        fixedChargeServiceMock.Setup(x => x.GetFixedChargeByIdAsync(9, user.Id))
            .ReturnsAsync(ServiceResult<FixedCharge>.Success(new FixedCharge
            {
                Id = 9,
                UserId = user.Id,
                Name = "Netflix",
                Description = "Streaming",
                Amount = 15.99m,
                CategoryId = 4,
                Frequency = "Monthly",
                DayOfMonth = 5,
                StartDate = DateTime.Today.AddMonths(-2),
                IsActive = true,
                AutoCreateExpense = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }));

        FixedChargeFormViewModel viewModel = new(
            fixedChargeServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync(new Dictionary<string, object>
        {
            [NavigationParameterKeys.FixedChargeId] = 9
        });

        Assert.IsTrue(viewModel.IsEditMode);
        Assert.AreEqual("Netflix", viewModel.Name);
        Assert.IsTrue(viewModel.AmountText is "15.99" or "15,99");
        Assert.AreEqual("5", viewModel.DayOfMonthText);
    }

    [TestMethod]
    public async Task SaveCommand_WhenDayInvalid_DoesNotCallService()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IFixedChargeService> fixedChargeServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 4, Name = "Abonnements", IsActive = true, IsSystem = true }
            }));

        FixedChargeFormViewModel viewModel = new(
            fixedChargeServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();
        viewModel.Name = "Netflix";
        viewModel.AmountText = "15";
        viewModel.SelectedCategoryId = 4;
        viewModel.DayOfMonthText = "32";

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        Assert.IsTrue(viewModel.HasValidationErrors);
        fixedChargeServiceMock.Verify(x => x.CreateFixedChargeAsync(It.IsAny<FixedCharge>()), Times.Never);
    }
}
