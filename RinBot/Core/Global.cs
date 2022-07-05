namespace RinBot.Core
{
    internal static class Global
    {
        public const string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public const string configPath = Path.Combine(currentDirectory, "config");
        public const string resourcePath = Path.Combine(currentDirectory, "resource");
        public const string databasePath = Path.Combine(currentDirectory, "database");
    }
}
