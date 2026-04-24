namespace MoneyMate.Services.Common;

public interface IMemoryCacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiration = null);
    void Remove(string key);
}

public class MemoryCacheService : IMemoryCacheService
{
    private readonly Dictionary<string, (object value, DateTime expiry)> _cache = new();

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.expiry > DateTime.UtcNow)
                return (T)entry.value;

            _cache.Remove(key);
        }

        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var expiry = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(5));
        _cache[key] = (value!, expiry);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }
}
