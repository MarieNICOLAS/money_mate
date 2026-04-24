using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Configuration;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels.Categories;

namespace UnitTests.ViewModels;

[TestClass]
public class CategoriesViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_WithCategories_PopulatesCollectionAndCounters()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 1, Name = "Alimentation", IsSystem = true, IsActive = true, Color = "#4CAF50", Icon = "🍎" },
                new() { Id = 2, UserId = user.Id, Name = "Maison", IsSystem = false, IsActive = true, Color = "#2196F3", Icon = "🏠" }
            }));

        categoryServiceMock.Setup(x => x.GetInactiveCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 3, UserId = user.Id, Name = "Voyage", IsSystem = false, IsActive = false, Color = "#9C27B0", Icon = "✈️" }
            }));

        CategoriesViewModel viewModel = new(
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual(3, viewModel.Categories.Count);
        Assert.AreEqual(2, viewModel.ActiveCategoriesCount);
        Assert.AreEqual(2, viewModel.CustomCategoriesCount);
        Assert.AreEqual(1, viewModel.InactiveCategoriesCount);
        Assert.IsTrue(viewModel.HasCategories);
        Assert.AreEqual("Alimentation", viewModel.Categories[0].Name);
    }

    [TestMethod]
    public async Task LoadAsync_WhenServiceFails_SetsErrorMessage()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Failure("CATEGORY_ERROR", "Impossible de charger les catégories."));

        CategoriesViewModel viewModel = new(
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual(0, viewModel.Categories.Count);
        Assert.IsTrue(viewModel.HasError);
        Assert.AreEqual("Impossible de charger les catégories.", viewModel.ErrorMessage);
    }

    [TestMethod]
    public async Task AddCategoryCommand_NavigatesToAddCategoryPage()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<INavigationService> navigationServiceMock = ViewModelTestHelper.CreateNavigationServiceMock();

        CategoriesViewModel viewModel = new(
            new Mock<ICategoryService>().Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            navigationServiceMock.Object);

        viewModel.AddCategoryCommand.Execute(null);
        await Task.Delay(100);

        navigationServiceMock.Verify(x => x.NavigateToAsync(AppRoutes.AddCategory), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCategoryCommand_WhenConfirmed_DeletesCategory()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<ICategoryService> categoryServiceMock = new();

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>()));

        categoryServiceMock.Setup(x => x.GetInactiveCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>()));

        categoryServiceMock.Setup(x => x.DeleteCategoryAsync(5, user.Id))
            .ReturnsAsync(ServiceResult.Success());

        CategoriesViewModel viewModel = new(
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock(confirmationResult: true).Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        viewModel.DeleteCategoryCommand.Execute(new CategoryItemViewModel
        {
            Id = 5,
            Name = "Maison",
            IsSystem = false,
            IsActive = true
        });

        await Task.Delay(100);

        categoryServiceMock.Verify(x => x.DeleteCategoryAsync(5, user.Id), Times.Once);
    }

    [TestMethod]
    public async Task LoadAsync_WithoutCurrentUser_SetsSessionError()
    {
        Mock<ICategoryService> categoryServiceMock = new();

        CategoriesViewModel viewModel = new(
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(null).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual("Aucune session utilisateur active.", viewModel.ErrorMessage);
        categoryServiceMock.Verify(x => x.GetCategoriesAsync(It.IsAny<int>()), Times.Never);
    }
}
