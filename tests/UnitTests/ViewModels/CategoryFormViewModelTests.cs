using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels;
using MoneyMate.ViewModels.Categories;

namespace UnitTests.ViewModels;

[TestClass]
public class CategoryFormViewModelTests
{
    [TestMethod]
    public async Task InitializeAsync_WithEditParameter_LoadsCategory()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<ICategoryService> categoryServiceMock = new();
        Mock<IAlertThresholdService> alertThresholdServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(12, user.Id))
            .ReturnsAsync(ServiceResult<Category>.Success(new Category
            {
                Id = 12,
                UserId = user.Id,
                Name = "Maison",
                Description = "Charges du foyer",
                Color = "#123456",
                Icon = "🏠",
                IsActive = true,
                IsSystem = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }));

        alertThresholdServiceMock.Setup(x => x.GetAlertThresholdsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<AlertThreshold>>.Success([]));

        CategoryFormViewModel viewModel = new(
            categoryServiceMock.Object,
            alertThresholdServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync(new Dictionary<string, object>
        {
            [NavigationParameterKeys.CategoryId] = 12
        });

        Assert.IsTrue(viewModel.IsEditMode);
        Assert.AreEqual(12, viewModel.EditingEntityId);
        Assert.AreEqual("Maison", viewModel.Name);
        Assert.AreEqual("#123456", viewModel.ColorHex);
        Assert.IsTrue(viewModel.CanDelete);
    }

    [TestMethod]
    public async Task SaveCommand_WhenValid_CreatesCategoryAndNavigates()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<ICategoryService> categoryServiceMock = new();
        Mock<IAlertThresholdService> alertThresholdServiceMock = new();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        categoryServiceMock.Setup(x => x.CreateCategoryAsync(It.IsAny<Category>()))
            .ReturnsAsync(ServiceResult<Category>.Success(new Category { Id = 1, UserId = user.Id, Name = "Sport" }));

        alertThresholdServiceMock.Setup(x => x.GetAlertThresholdsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<AlertThreshold>>.Success([]));

        CategoryFormViewModel viewModel = new(
            categoryServiceMock.Object,
            alertThresholdServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        await viewModel.InitializeAsync();
        viewModel.Name = "Sport";
        viewModel.ColorHex = "#654321";
        viewModel.Icon = "⚽";

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        categoryServiceMock.Verify(x => x.CreateCategoryAsync(It.Is<Category>(category =>
            category.UserId == user.Id &&
            category.Name == "Sport" &&
            category.Color == "#654321")), Times.Once);
        navigationServiceMock.Verify(x => x.NavigateToAsync(AppRoutes.CategoriesList), Times.Once);
    }

    [TestMethod]
    public async Task SaveCommand_WhenEditingSystemCategory_CustomizesCategoryAndNavigates()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<ICategoryService> categoryServiceMock = new();
        Mock<IAlertThresholdService> alertThresholdServiceMock = new();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(12, user.Id))
            .ReturnsAsync(ServiceResult<Category>.Success(new Category
            {
                Id = 12,
                Name = "Alimentation",
                Description = "Système",
                Color = "#123456",
                Icon = "🛒",
                IsActive = true,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }));

        categoryServiceMock.Setup(x => x.CustomizeSystemCategoryAsync(It.IsAny<Category>()))
            .ReturnsAsync(ServiceResult<Category>.Success(new Category
            {
                Id = 99,
                UserId = user.Id,
                ParentCategoryId = 12,
                Name = "Courses",
                IsSystem = false
            }));

        alertThresholdServiceMock.Setup(x => x.GetAlertThresholdsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<AlertThreshold>>.Success([]));

        CategoryFormViewModel viewModel = new(
            categoryServiceMock.Object,
            alertThresholdServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        await viewModel.InitializeAsync(new Dictionary<string, object>
        {
            [NavigationParameterKeys.CategoryId] = 12
        });

        viewModel.Name = "Courses";
        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        categoryServiceMock.Verify(x => x.CustomizeSystemCategoryAsync(It.Is<Category>(category =>
            category.Id == 12 &&
            category.UserId == user.Id &&
            category.Name == "Courses")), Times.Once);
        categoryServiceMock.Verify(x => x.UpdateCategoryAsync(It.IsAny<Category>()), Times.Never);
        navigationServiceMock.Verify(x => x.NavigateToAsync(AppRoutes.CategoriesList), Times.Once);
    }

    [TestMethod]
    public async Task SaveCommand_WhenInvalid_DoesNotCallService()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<ICategoryService> categoryServiceMock = new();
        Mock<IAlertThresholdService> alertThresholdServiceMock = new();

        CategoryFormViewModel viewModel = new(
            categoryServiceMock.Object,
            alertThresholdServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.InitializeAsync();
        viewModel.Name = string.Empty;

        viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        Assert.IsTrue(viewModel.HasValidationErrors);
        categoryServiceMock.Verify(x => x.CreateCategoryAsync(It.IsAny<Category>()), Times.Never);
    }
}
