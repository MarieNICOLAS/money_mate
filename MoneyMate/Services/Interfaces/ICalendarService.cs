using MoneyMate.Models.DTOs;

namespace MoneyMate.Services.Interfaces;

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarOperationDto>> GetOperationsForMonthAsync(int userId, DateTime month);

    Task<IReadOnlyList<CalendarOperationDto>> GetOperationsForDayAsync(int userId, DateTime date);
}
