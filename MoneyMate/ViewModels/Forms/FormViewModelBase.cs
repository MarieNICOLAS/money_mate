using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MoneyMate.Models;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.ViewModels.Forms;

/// <summary>
/// Base commune pour les ViewModels de formulaires création / édition.
/// </summary>
public abstract class FormViewModelBase : AuthenticatedViewModelBase
{
    private int _editingEntityId;
    private bool _isEditMode;
    private string _validationMessage = string.Empty;
    private bool _canSave;
    private bool _canDelete;

    protected FormViewModelBase(
        IAuthenticationService authenticationService,
        IDialogService dialogService,
        INavigationService navigationService)
        : base(authenticationService, dialogService, navigationService)
    {
        SaveCommand = new Command(async () => await SaveAsync(), () => CanSave);
        CancelCommand = new Command(async () => await CancelAsync(), () => !IsBusy);
        DeleteCommand = new Command(async () => await DeleteAsync(), () => CanDelete);
    }

    public int EditingEntityId
    {
        get => _editingEntityId;
        protected set => SetProperty(ref _editingEntityId, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        protected set => SetProperty(ref _isEditMode, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        protected set
        {
            if (SetProperty(ref _validationMessage, value))
                OnPropertyChanged(nameof(HasValidationErrors));
        }
    }

    public bool HasValidationErrors => !string.IsNullOrWhiteSpace(ValidationMessage);

    public bool CanSave
    {
        get => _canSave;
        private set => SetProperty(ref _canSave, value);
    }

    public bool CanDelete
    {
        get => _canDelete;
        private set => SetProperty(ref _canDelete, value);
    }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand DeleteCommand { get; }

    protected virtual bool CanDeleteEntity => IsEditMode;

    public async Task InitializeAsync(Dictionary<string, object>? parameters = null)
    {
        await ExecuteBusyActionAsync(async () =>
        {
            if (!EnsureCurrentUser())
            {
                RefreshFormState();
                return;
            }

            await LoadLookupsAsync();

            if (TryGetEntityId(parameters, EditParameterKey, out int entityId))
            {
                IsEditMode = true;
                EditingEntityId = entityId;
                await LoadForEditAsync(entityId);
            }
            else
            {
                IsEditMode = false;
                EditingEntityId = 0;
                await InitializeForCreateAsync();
            }

            RefreshFormState();
        }, "Une erreur est survenue lors de l'initialisation du formulaire.");
    }

    protected virtual Task LoadLookupsAsync() => Task.CompletedTask;

    protected virtual Task InitializeForCreateAsync() => Task.CompletedTask;

    protected abstract string EditParameterKey { get; }

    protected abstract Task LoadForEditAsync(int entityId);

    protected abstract string ValidateForm();

    protected abstract Task<bool> SaveCoreAsync();

    protected virtual Task<bool> DeleteCoreAsync() => Task.FromResult(false);

    protected bool SetFormProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        bool changed = SetProperty(ref field, value, propertyName);
        if (changed)
            RefreshFormState();

        return changed;
    }

    protected void RefreshFormState()
    {
        ValidationMessage = ValidateForm();
        CanSave = !IsBusy && !HasValidationErrors;
        CanDelete = !IsBusy && CanDeleteEntity;

        if (SaveCommand is Command saveCommand)
            saveCommand.ChangeCanExecute();

        if (DeleteCommand is Command deleteCommand)
            deleteCommand.ChangeCanExecute();

        if (CancelCommand is Command cancelCommand)
            cancelCommand.ChangeCanExecute();
    }

    public static bool TryGetEntityId(Dictionary<string, object>? parameters, string key, out int entityId)
    {
        entityId = 0;

        if (parameters == null || !parameters.TryGetValue(key, out object? value) || value == null)
            return false;

        if (value is int id && id > 0)
        {
            entityId = id;
            return true;
        }

        if (value is string stringValue && int.TryParse(stringValue, out int parsedId) && parsedId > 0)
        {
            entityId = parsedId;
            return true;
        }

        return false;
    }

    protected static bool TryParseDecimalInput(string? text, out decimal value)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            return true;

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;

        string normalized = (text ?? string.Empty).Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsBusy))
            RefreshFormState();
    }

    private async Task SaveAsync()
    {
        RefreshFormState();

        if (!EnsureCurrentUser())
            return;

        if (HasValidationErrors)
        {
            ErrorMessage = ValidationMessage;
            return;
        }

        await ExecuteBusyActionAsync(async () =>
        {
            await SaveCoreAsync();
            RefreshFormState();
        }, "Une erreur est survenue lors de l'enregistrement du formulaire.");
    }

    private async Task CancelAsync()
    {
        if (IsBusy)
            return;

        await NavigationService.GoBackAsync();
    }

    private async Task DeleteAsync()
    {
        if (!CanDelete)
            return;

        await ExecuteBusyActionAsync(async () =>
        {
            await DeleteCoreAsync();
            RefreshFormState();
        }, "Une erreur est survenue lors de la suppression.");
    }
}

/// <summary>
/// Élément de sélection de catégorie pour les formulaires.
/// </summary>
public sealed class CategoryOptionViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsSystem { get; init; }

    public static CategoryOptionViewModel FromModel(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryOptionViewModel
        {
            Id = category.Id,
            Name = category.Name,
            IsSystem = category.IsSystem
        };
    }
}

/// <summary>
/// Élément de sélection de budget pour les formulaires.
/// </summary>
public sealed class BudgetOptionViewModel
{
    public int Id { get; init; }

    public int CategoryId { get; init; }

    public string Label { get; init; } = string.Empty;

    public static BudgetOptionViewModel FromModel(Budget budget, string categoryName)
    {
        ArgumentNullException.ThrowIfNull(budget);

        return new BudgetOptionViewModel
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            Label = $"{categoryName} • {budget.Amount:N2}"
        };
    }
}
