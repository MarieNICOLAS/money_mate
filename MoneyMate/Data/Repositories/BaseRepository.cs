using SQLite;
using System.Linq.Expressions;
using MoneyMate.Data.Context;

namespace MoneyMate.Data.Repositories;

public class BaseRepository<T> : IDataRepository<T> where T : class, new()
{
    private readonly IMoneyMateDbContext _context;

    public BaseRepository(IMoneyMateDbContext context)
    {
        _context = context;
    }

    private SQLiteConnection GetConnection()
    {
        return _context is MoneyMateDbContext dbContext
            ? dbContext.GetConnectionSafe()
            : throw new InvalidOperationException("DB connection error");
    }

    public Task<List<T>> GetAllAsync()
        => Task.Run(() =>
        {
            var conn = GetConnection();
            return conn.Table<T>().ToList();
        });

    public Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => Task.Run(() =>
        {
            var conn = GetConnection();
            return conn.Table<T>().Where(predicate).ToList();
        });

    public Task<T?> GetByIdAsync(int id)
        => Task.Run(() =>
        {
            var conn = GetConnection();
            return conn.Find<T>(id);
        });

    public Task<int> InsertAsync(T entity)
        => Task.Run(() =>
        {
            var conn = GetConnection();
            return conn.Insert(entity);
        });

    public Task<int> UpdateAsync(T entity)
        => Task.Run(() =>
        {
            var conn = GetConnection();
            return conn.Update(entity);
        });

    public Task<int> DeleteAsync(T entity)
        => Task.Run(() =>
        {
            var conn = GetConnection();
            return conn.Delete(entity);
        });
}
