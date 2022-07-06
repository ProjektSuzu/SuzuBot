using RinBot.Core.Component.Database;

namespace RinBot.Core.Component.EnvVar
{
    internal class EnvVarManager
    {
        #region Singleton
        private static EnvVarManager instance;
        public static EnvVarManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new();
                return instance;
            }
        }
        private EnvVarManager()
        {
            database.dbConnection.CreateTable<EnvironmentVariable>();
        }
        #endregion
        private readonly RinDatabase database = RinDatabase.Instance;

        public string GetEnvVar(string key)
        {
            return database.dbConnection
                .Table<EnvironmentVariable>()
                .FirstOrDefault(x => x.Key == key)
                ?.Value ?? "";
        }

        public string SetEnvVar(string key, string value)
        {
            database.dbConnection.InsertOrReplace(new EnvironmentVariable() { Key = key, Value = value });
            return GetEnvVar(key);
        }

        public bool DelEnvVar(string key)
        {
            return database.dbConnection
                .Table<EnvironmentVariable>()
                .Where(x => x.Key == key)
                .Delete() > 1;
        }

        public bool HasEnvVar(string key)
        {
            return database.dbConnection
                .Table<EnvironmentVariable>()
                .Where(x => x.Key == key)
                .Any();
        }
    }
}
