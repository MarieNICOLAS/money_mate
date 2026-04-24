using MoneyMate.Data.Repositories;
using MoneyMate.Models;
using MoneyMate.Services.Common;

namespace MoneyMate.Data.Context;

public class DatabaseService
{
    private readonly IDataRepository<User> _userRepo;
    private readonly IDataRepository<Expense> _expenseRepo;
    private readonly IMemoryCacheService _cache;

    public DatabaseService(
        IDataRepository<User> userRepo,
        IDataRepository<Expense> expenseRepo,
        IMemoryCacheService cache)
    {
        _userRepo = userRepo;
        _expenseRepo = expenseRepo;
        _cache = cache;
    }

    // ========================
    // USERS (cached)
    // ========================
    public async Task<List<User>> GetUsersAsync()
    {
        const string cacheKey = "users";

        var cached = _cache.Get<List<User>>(cacheKey);
        if (cached != null)
            return cached;

        var users = await _userRepo.GetAllAsync();

        _cache.Set(cacheKey, users);

        return users;
    }

    // ========================
    // EXPENSES (optimized)
    // ========================
    public async Task<List<Expense>> GetExpensesByUserAsync(int userId)
    {
        var cacheKey = $"expenses_{userId}";

        var cached = _cache.Get<List<Expense>>(cacheKey);
        if (cached != null)
            return cached;

        var data = await _expenseRepo.FindAsync(e => e.UserId == userId);

        var result = data
            .OrderByDescending(e => e.DateOperation)
            .Take(200) // 🔥 LIMIT PERF
            .ToList();

        _cache.Set(cacheKey, result);

        return result;
    }

    public void InvalidateUserCache(int userId)
    {
        _cache.Remove($"expenses_{userId}");
    }
}
