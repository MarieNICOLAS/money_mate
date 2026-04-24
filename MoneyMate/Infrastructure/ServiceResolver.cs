namespace MoneyMate.Infrastructure;

public static class ServiceResolver
{
    private static IServiceProvider? _serviceProvider;

    public static void Configure(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public static T GetRequiredService<T>() where T : notnull
        => (T)(_serviceProvider?.GetService(typeof(T))
            ?? throw new InvalidOperationException($"Le service '{typeof(T).FullName}' est indisponible."));
}
