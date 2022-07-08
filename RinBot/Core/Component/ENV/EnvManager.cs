using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using RinBot.Core.Component.Database;

namespace RinBot.Core.Component.ENV
{
    internal class EnvManager
    {
        #region Singleton
        private static EnvManager instance;
        public static EnvManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new();
                return instance;
            }
        }
        private EnvManager()
        {
            database.dbConnection.CreateTable<EnvironmentVariable>();
        }
        #endregion
        private readonly RinDatabase database = RinDatabase.Instance;

        public int GetEnvCount()
            => database.dbConnection
            .Table<EnvironmentVariable>()
            .Count();

        public List<string> GetEnv(string key)
        {
            return database.dbConnection
                .Table<EnvironmentVariable>()
                .FirstOrDefault(x => x.Key == key)
                .Value ?? new();
        }

        public List<EnvironmentVariable> GetEnvs()
            => database.dbConnection.Table<EnvironmentVariable>().ToList();

        public bool SetEnv(string key, string value)
        {
            return database.dbConnection.InsertOrReplace(new EnvironmentVariable() { Key = key, Value = new() { value } }) > 0;
        }

        public bool SetEnv(string key, List<string> values)
        {
            return database.dbConnection.InsertOrReplace(new EnvironmentVariable() { Key = key, Value = values }) > 0;
        }

        public bool AddEnv(string key, string newValue)
        {
            var env = database.dbConnection
                .Table<EnvironmentVariable>()
                .First(x => x.Key == key);
            env.Value.Add(newValue);
            return database.dbConnection
                .InsertOrReplace(env) > 0;
        }

        public bool DelEnv(string key)
        {
            return database.dbConnection
                .Table<EnvironmentVariable>()
                .Where(x => x.Key == key)
                .Delete() > 1;
        }

        public bool HasEnv(string key)
        {
            return database.dbConnection
                .Table<EnvironmentVariable>()
                .Where(x => x.Key == key)
                .Any();
        }
    }
}
