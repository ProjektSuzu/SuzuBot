
using Konata.Core.Common;
using Konata.Core.Events.Model;
using Newtonsoft.Json;
using System.Text;

namespace ProjektRin.System
{
    public class GroupManager
    {
        private static GroupManager _instance = new GroupManager();
        private GroupManager()
        {
            try
            {
                LoadPreferences();
            }
            catch { }
            finally { SavePreferences(); }
        }
        public static GroupManager Instance => _instance;

        string rootPath = AppDomain.CurrentDomain.BaseDirectory;

        private static CommandManager commandManager = CommandManager.Instance;

        internal List<GroupPreference> groupPreference;

        private void LoadPreferences()
        {
            var json = File.ReadAllText(rootPath + "/groupPreferences.json");
            groupPreference = JsonConvert.DeserializeObject<List<GroupPreference>>(json) ?? new();
        }

        private void SavePreferences()
        {
            var json = JsonConvert.SerializeObject(groupPreference);
            File.WriteAllText(rootPath + "/groupPreferences.json", json, Encoding.UTF8);
        }

        internal GroupPreference GetPreference(uint groupUin)
        {
            var preference = groupPreference.Find(x => x.GroupUin == groupUin) ?? null;
            if (preference == null)
            {
                preference = new GroupPreference(groupUin);
                groupPreference.Add(preference);
                SavePreferences();
            }
            return preference;
        }

        public bool IsPassiveMode(uint groupUin)
        {
            var preference = GetPreference(groupUin);
            return preference.PassiveMode;
        }

        public bool SetDisabledCommandSet(uint groupUin, bool action, string commandSet)
        {
            var preference = GetPreference(groupUin);
            if (!commandManager.HasCommandSet(commandSet))
            {
                throw new InvalidOperationException($"命令集 \"{commandSet}\" 不存在.");
            }

            if (commandSet == "CoreCommands")
            {
                throw new InvalidOperationException($"命令集 \"{commandSet}\" 是核心部件.");
            }

            if (action)
            {
                if (!preference.DisabledCommandSets.Remove(commandSet)) return false;
            }
            else
            {
                if (preference.DisabledCommandSets.Contains(commandSet)) return false;
                else preference.DisabledCommandSets.Add(commandSet);
            }
            SavePreferences();
            return true;
        }

        public bool IsCommandSetDisabled(uint groupUin, string commandSet)
        {
            var preference = GetPreference(groupUin);
            return preference.DisabledCommandSets.Contains(commandSet);
        }

        public bool TogglePassiveMode(uint groupUin)
        {
            var preference = GetPreference(groupUin);
            preference.PassiveMode = !preference.PassiveMode;
            SavePreferences();
            return preference.PassiveMode;
        }

        public bool SetPassiveMode(uint groupUin, bool value)
        {
            var preference = GetPreference(groupUin);
            preference.PassiveMode = value;
            SavePreferences();
            return preference.PassiveMode;
        }
    }
}