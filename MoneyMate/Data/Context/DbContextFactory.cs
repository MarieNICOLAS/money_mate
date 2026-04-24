namespace MoneyMate.Data.Context;

public static class DbContextFactory
{
    public static IMoneyMateDbContext CreateDefault()
    {
        string appDataDirectory;

        try
        {
            appDataDirectory = FileSystem.AppDataDirectory;
        }
        catch
        {
            appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        string databasePath = Path.Combine(appDataDirectory, "money_mate.db");
        return new MoneyMateDbContext(databasePath);
    }
}
