using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
}
