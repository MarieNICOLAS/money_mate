using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels.Budgets;

namespace UnitTests.ViewModels;

[TestClass]
public class BudgetsOverviewViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_WithBudgets_ComputesTotalsAndRiskCount()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        Budget budget1 = new() { Id = 1, UserId = user.Id, CategoryId = 100, Amount = 100m, PeriodType = "Monthly", StartDate = DateTime.Today.AddDays(-15) };
        Budget budget2 = new() { Id = 2, UserId = user.Id, CategoryId = 200, Amount = 200m, PeriodType = "Monthly", StartDate = DateTime.Today.AddDays(-20) };

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success(new List<Budget> { budget1, budget2 }));

        budgetServiceMock.Setup(x => x.GetBudgetConsumptionSummaryAsync(1, user.Id))
            .ReturnsAsync(ServiceResult<BudgetConsumptionSummary>.Success(new BudgetConsumptionSummary
            {
                Budget = budget1,
                ConsumedAmount = 90m,
                RemainingAmount = 10m,
                ConsumedPercentage = 90m,
                IsExceeded = false
            }));

        budgetServiceMock.Setup(x => x.GetBudgetConsumptionSummaryAsync(2, user.Id))
            .ReturnsAsync(ServiceResult<BudgetConsumptionSummary>.Success(new BudgetConsumptionSummary
            {
                Budget = budget2,
                ConsumedAmount = 50m,
                RemainingAmount = 150m,
                ConsumedPercentage = 25m,
                IsExceeded = false
            }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 100, Name = "Courses", Color = "#4CAF50", Icon = "🛒", IsActive = true },
                new() { Id = 200, Name = "Transport", Color = "#2196F3", Icon = "🚗", IsActive = true }
            }));

        BudgetsOverviewViewModel viewModel = new(
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.Budgets.Count);
        Assert.AreEqual(300m, viewModel.TotalBudgetAmount);
        Assert.AreEqual(140m, viewModel.TotalConsumedAmount);
        Assert.AreEqual(2, viewModel.ActiveBudgetsCount);
        Assert.AreEqual(1, viewModel.BudgetsAtRiskCount);
        Assert.IsTrue(viewModel.HasBudgets);
        Assert.AreEqual("Courses", viewModel.Budgets[0].CategoryName);
    }
}
