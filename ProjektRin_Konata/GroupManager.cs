
using Newtonsoft.Json;
using System.Text;

namespace ProjektRin
{
    public class GroupManager
    {
        private static GroupManager _instance = new GroupManager();
        private GroupManager() 
        {
            try
            {
                LoadList();
            } catch {  }
            finally { SaveList(); }
        }
        public static GroupManager Instance => _instance;

        string rootPath = Directory.GetCurrentDirectory();


        List<uint> _passiveModeList = new(114514);

        private void LoadList()
        {
            var json = File.ReadAllText(rootPath + "/groupPreference.json");
            _passiveModeList = JsonConvert.DeserializeObject<List<uint>>(json) ?? new List<uint>();
        }

        private void SaveList()
        {
            var json = JsonConvert.SerializeObject(_passiveModeList);
            File.WriteAllText(rootPath + "/groupPreference.json", json, Encoding.UTF8);
        }

        public bool IsPassiveMode(uint groupUin)
        {
            return _passiveModeList.Contains(groupUin);
        }

        public void EnablePassiveMode(uint groupUin)
        {
            if (!IsPassiveMode(groupUin))
                _passiveModeList.Add(groupUin);
            SaveList();
        }

        public void DisablePassiveMode(uint groupUin)
        {
            _passiveModeList.Remove(groupUin);
            SaveList();
        }
    }
}