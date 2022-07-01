namespace RinBot.Core
{
    internal static class Global
    {
        public static string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string configPath = Path.Combine(currentDirectory, "config");
        public static string resourcePath = Path.Combine(currentDirectory, "resource");
        public static string databasePath = Path.Combine(currentDirectory, "database");
    }
}
