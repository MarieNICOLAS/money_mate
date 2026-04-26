using MoneyMate.Services.Interfaces;

namespace MoneyMate.Services.Implementations;

/// <summary>
/// Bus léger de changement applicatif basé sur des versions par domaine.
/// </summary>
public sealed class AppEventBus : IAppEventBus
{
    private readonly Lock _syncRoot = new();
    private readonly Dictionary<AppDataChangeKind, long> _versions = new()
    {
        [AppDataChangeKind.Expenses] = 0,
        [AppDataChangeKind.Budgets] = 0,
        [AppDataChangeKind.Categories] = 0,
        [AppDataChangeKind.FixedCharges] = 0,
        [AppDataChangeKind.AlertThresholds] = 0
    };

    private long _version;

    /// <inheritdoc />
    public void PublishDataChanged(AppDataChangeKind changeKinds)
    {
        if (changeKinds == AppDataChangeKind.None)
            return;

        lock (_syncRoot)
        {
            _version++;

            foreach (AppDataChangeKind changeKind in EnumerateSingleKinds(changeKinds))
                _versions[changeKind] = _version;
        }

        System.Diagnostics.Debug.WriteLine($"AppEventBus: données modifiées ({changeKinds}).");
    }

    /// <inheritdoc />
    public bool HasChangedSince(AppDataChangeKind changeKinds, long observedVersion)
        => GetVersion(changeKinds) > observedVersion;

    /// <inheritdoc />
    public long GetVersion(AppDataChangeKind changeKinds)
    {
        if (changeKinds == AppDataChangeKind.None)
            return 0;

        lock (_syncRoot)
        {
            long version = 0;

            foreach (AppDataChangeKind changeKind in EnumerateSingleKinds(changeKinds))
            {
                if (_versions.TryGetValue(changeKind, out long changeVersion))
                    version = Math.Max(version, changeVersion);
            }

            return version;
        }
    }

    private static IEnumerable<AppDataChangeKind> EnumerateSingleKinds(AppDataChangeKind changeKinds)
    {
        AppDataChangeKind[] singleKinds =
        [
            AppDataChangeKind.Expenses,
            AppDataChangeKind.Budgets,
            AppDataChangeKind.Categories,
            AppDataChangeKind.FixedCharges,
            AppDataChangeKind.AlertThresholds
        ];

        foreach (AppDataChangeKind singleKind in singleKinds)
        {
            if (changeKinds.HasFlag(singleKind))
                yield return singleKind;
        }
    }
}
