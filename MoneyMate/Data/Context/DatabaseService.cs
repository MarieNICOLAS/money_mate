namespace MoneyMate.Data.Context
{
    /// <summary>
    /// Service de configuration de la base SQLite.
    /// Fournit une instance singleton partagée du contexte.
    /// </summary>
    public static class DatabaseService
    {
        private static readonly object SyncRoot = new();
        private static IMoneyMateDbContext? _instance;

        /// <summary>
        /// Instance singleton du contexte de base de données.
        /// </summary>
        public static IMoneyMateDbContext Instance
        {
            get
            {
                if (_instance is not null)
                    return _instance;

                lock (SyncRoot)
                {
                    if (_instance is not null)
                        return _instance;

                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string dbPath = Path.Combine(localAppData, "MoneyMate.db3");

                    _instance = new Data.Context.MoneyMateDbContext(dbPath);
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Ferme explicitement la connexion SQLite.
        /// </summary>
        public static void CloseConnection()
        {
            lock (SyncRoot)
            {
                if (_instance is IDisposable disposable)
                    disposable.Dispose();

                _instance = null;
            }
        }
    }
}
