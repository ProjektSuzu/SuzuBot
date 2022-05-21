using Newtonsoft.Json;

namespace RinBot.Core.Components
{
    internal class GroupPreferenceManager
    {
        #region 单例模式
        private static GroupPreferenceManager instance;
        private GroupPreferenceManager()
        {
            Load();
        }
        public static GroupPreferenceManager Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        #endregion

        private static readonly string groupPreferencePath = Path.Combine(BotManager.configPath, "groupPreference.json");
        private List<GroupPreference> groupPreferences;

        public void Save()
        {
            File.WriteAllText(groupPreferencePath, JsonConvert.SerializeObject(groupPreferences));
        }

        public void Load()
        {
            if (File.Exists(groupPreferencePath))
            {
                groupPreferences = JsonConvert.DeserializeObject<List<GroupPreference>>(File.ReadAllText(groupPreferencePath));
            }
            else
            {
                groupPreferences = new();
                Save();
            }
        }

        public GroupPreference GetPreference(uint groupUin)
        {
            var preference = groupPreferences.FirstOrDefault(x => x.GroupUin == groupUin, null);
            if (preference != null) return preference;

            preference = new GroupPreference()
            {
                GroupUin = groupUin
            };
            groupPreferences.Add(preference);
            Save();
            return preference;
        }


        public bool IsCommandSetEnabled(uint groupUin, string packageName)
        {
            if (packageName == "") return false;
            var preference = GetPreference(groupUin);
            bool enabled;
            if (preference.CommandSetPreferences.TryGetValue(packageName, out enabled))
            {
                return enabled;
            }
            else
            {
                var loadedCommandSet = CommandManager.Instance.CommandSets.FirstOrDefault(x => x.CommandSetAttr.PackageName == packageName, null);
                if (loadedCommandSet == null) return false;

                preference.CommandSetPreferences.Add(packageName, loadedCommandSet.CommandSetAttr.DefaultEnable);
                Save();
                return loadedCommandSet.CommandSetAttr.DefaultEnable;
            }
        }
    }

    internal class GroupPreference
    {
        public uint GroupUin;
        public bool SilentMode;
        public Dictionary<string, bool> CommandSetPreferences = new();
    }
}
