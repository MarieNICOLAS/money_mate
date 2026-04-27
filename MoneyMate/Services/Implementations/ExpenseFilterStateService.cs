using MoneyMate.Models.DTOs;
using MoneyMate.Services.Interfaces;

namespace MoneyMate.Services.Implementations;

public sealed class ExpenseFilterStateService : IExpenseFilterStateService
{
    private readonly Lock _syncRoot = new();
    private ExpenseFilterDto _currentFilter = new();
    private long _version;

    public ExpenseFilterDto CurrentFilter
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentFilter.Clone();
            }
        }
    }

    public long Version
    {
        get
        {
            lock (_syncRoot)
            {
                return _version;
            }
        }
    }

    public void SetFilter(ExpenseFilterDto filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        lock (_syncRoot)
        {
            _currentFilter = filter.Clone();
            _version++;
        }
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            _currentFilter = new ExpenseFilterDto();
            _version++;
        }
    }
}
