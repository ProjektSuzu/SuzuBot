namespace RinBot.Core
{
    internal static class Global
    {
        public static readonly string ROOT_PATH = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string CONFIG_PATH = Path.Combine(ROOT_PATH, "config");
        public static readonly string RESOURCE_PATH = Path.Combine(ROOT_PATH, "resource");
        public static readonly string DB_PATH = Path.Combine(ROOT_PATH, "database");
    }
}
