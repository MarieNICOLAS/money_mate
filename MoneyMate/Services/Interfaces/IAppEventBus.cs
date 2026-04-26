namespace MoneyMate.Services.Interfaces;

/// <summary>
/// Suit les changements applicatifs utiles aux rafraîchissements MVVM.
/// </summary>
public interface IAppEventBus
{
    /// <summary>
    /// Publie un changement de données pour un ou plusieurs domaines.
    /// </summary>
    /// <param name="changeKinds">Domaines applicatifs modifiés.</param>
    void PublishDataChanged(AppDataChangeKind changeKinds);

    /// <summary>
    /// Indique si un domaine a changé depuis une version observée.
    /// </summary>
    /// <param name="changeKinds">Domaines à surveiller.</param>
    /// <param name="observedVersion">Version déjà observée par un ViewModel.</param>
    /// <returns>True si au moins un domaine a changé.</returns>
    bool HasChangedSince(AppDataChangeKind changeKinds, long observedVersion);

    /// <summary>
    /// Retourne la version la plus récente des domaines demandés.
    /// </summary>
    /// <param name="changeKinds">Domaines à consulter.</param>
    /// <returns>Version la plus récente.</returns>
    long GetVersion(AppDataChangeKind changeKinds);
}

/// <summary>
/// Domaines de données pouvant impacter les écrans connectés.
/// </summary>
[Flags]
public enum AppDataChangeKind
{
    None = 0,
    Expenses = 1,
    Budgets = 2,
    Categories = 4,
    FixedCharges = 8,
    AlertThresholds = 16,
    All = Expenses | Budgets | Categories | FixedCharges | AlertThresholds
}

/// <summary>
/// Implémentation neutre utilisée par les tests ou les constructions directes.
/// </summary>
public sealed class NullAppEventBus : IAppEventBus
{
    public static NullAppEventBus Instance { get; } = new();

    private NullAppEventBus()
    {
    }

    /// <inheritdoc />
    public void PublishDataChanged(AppDataChangeKind changeKinds)
    {
    }

    /// <inheritdoc />
    public bool HasChangedSince(AppDataChangeKind changeKinds, long observedVersion) => false;

    /// <inheritdoc />
    public long GetVersion(AppDataChangeKind changeKinds) => 0;
}
