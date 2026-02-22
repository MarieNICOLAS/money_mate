namespace MoneyMate.Data.Context
{
    /// <summary>
    /// Service de configuration de la base de données
    /// Gère l'initialisation et la configuration SQLite
    /// </summary>
    public static class DatabaseService
    {
        private static MoneyMateDbContext? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// Obtient l'instance singleton du contexte de base de données
        /// </summary>
        public static MoneyMateDbContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // Utilisation du dossier AppData\Local de l'utilisateur
                            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            var dbPath = Path.Combine(localAppData, "MoneyMate.db3");
                            _instance = new MoneyMateDbContext(dbPath);
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Ferme la connexion à la base de données
        /// </summary>
        public static void CloseConnection()
        {
            _instance?.Close();
            _instance = null;
        }
    }
}