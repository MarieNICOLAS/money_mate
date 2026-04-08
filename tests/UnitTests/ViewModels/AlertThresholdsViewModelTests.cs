using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;
using MoneyMate.Services.Models;
using MoneyMate.Services.Results;
using MoneyMate.ViewModels.Alerts;

namespace UnitTests.ViewModels;

[TestClass]
public class AlertThresholdsViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_WithAlerts_ComputesTriggeredCountAndTargetNames()
    {
        User user = ViewModelTestHelper.CreateUser();
        Mock<IAlertThresholdService> alertServiceMock = new();
        Mock<IBudgetService> budgetServiceMock = new();
        Mock<ICategoryService> categoryServiceMock = new();

        Budget budget = new() { Id = 5, UserId = user.Id, CategoryId = 100, Amount = 150m, PeriodType = "Monthly", StartDate = DateTime.Today.AddDays(-10) };
        AlertThreshold warningAlert = new() { Id = 1, UserId = user.Id, BudgetId = 5, ThresholdPercentage = 80m, AlertType = "Warning", IsActive = true };
        AlertThreshold criticalAlert = new() { Id = 2, UserId = user.Id, CategoryId = 100, ThresholdPercentage = 95m, AlertType = "Critical", IsActive = true };

        alertServiceMock.Setup(x => x.GetAlertThresholdsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<AlertThreshold>>.Success(new List<AlertThreshold> { warningAlert, criticalAlert }));

        alertServiceMock.Setup(x => x.EvaluateAlertAsync(1, user.Id))
            .ReturnsAsync(ServiceResult<AlertTriggerInfo>.Success(new AlertTriggerInfo
            {
                AlertThreshold = warningAlert,
                Budget = budget,
                BudgetAmount = 150m,
                ConsumedAmount = 130m,
                ConsumedPercentage = 86m,
                IsTriggered = true
            }));

        alertServiceMock.Setup(x => x.EvaluateAlertAsync(2, user.Id))
            .ReturnsAsync(ServiceResult<AlertTriggerInfo>.Success(new AlertTriggerInfo
            {
                AlertThreshold = criticalAlert,
                Budget = budget,
                BudgetAmount = 150m,
                ConsumedAmount = 60m,
                ConsumedPercentage = 40m,
                IsTriggered = false
            }));

        budgetServiceMock.Setup(x => x.GetBudgetsAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Budget>>.Success(new List<Budget> { budget }));

        categoryServiceMock.Setup(x => x.GetCategoriesAsync(user.Id))
            .ReturnsAsync(ServiceResult<List<Category>>.Success(new List<Category>
            {
                new() { Id = 100, Name = "Courses", Color = "#4CAF50", Icon = "🛒", IsActive = true }
            }));

        AlertThresholdsViewModel viewModel = new(
            alertServiceMock.Object,
            budgetServiceMock.Object,
            categoryServiceMock.Object,
            ViewModelTestHelper.CreateAuthenticationServiceMock(user).Object,
            ViewModelTestHelper.CreateDialogServiceMock().Object,
            ViewModelTestHelper.CreateNavigationServiceMock().Object);

        await viewModel.LoadAsync();

        Assert.AreEqual(2, viewModel.Alerts.Count);
        Assert.AreEqual(2, viewModel.ActiveAlertsCount);
        Assert.AreEqual(1, viewModel.TriggeredAlertsCount);
        Assert.IsTrue(viewModel.HasAlerts);
        Assert.IsTrue(viewModel.Alerts.Any(alert => alert.TargetName == "Budget • Courses" && alert.CurrentPercentage == 86m));
        Assert.IsTrue(viewModel.Alerts.Any(alert => alert.TargetName == "Catégorie • Courses" && alert.CurrentPercentage == 40m));
    }
}
