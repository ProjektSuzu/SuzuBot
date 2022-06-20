using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NLog;
using RinBot.Core.Components;
using RinBot.Utils.Database;
using RinBot.Utils.Database.Tables;
using System.Text;

namespace RinBot.Commands.Modules.Adventure
{
    internal static class AdventureManager
    {
        private static readonly string TAG = "ADVMGR";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        private static readonly string eventDirPath = Path.Combine(BotManager.resourcePath, "AdvEvent");

        //eventID = mainEventID-subEventID
        //subEventID = Event000 为起始事件    

        public static AdvState ExecEvent(string eventID, ref UserInfo info)
        {
            var mainEventID = eventID.Split('-')[0];
            var subEventID = eventID.Split('-')[1];
            var script = CSharpScript.Create(File.ReadAllText(Path.Combine(eventDirPath, mainEventID + ".cs")), ScriptOptions.Default, typeof(AdvData));
            var data = new AdvData
            {
                info = info,
                state = new AdvState()
            };

            script = script.ContinueWith($"{subEventID}();");
            script.RunAsync(data).Wait();
            Console.WriteLine(data.state.CoolDown);

            info = data.info;
            return data.state;
        }

        public static AdvState StartNewEvent(ref UserInfo info)
        {
            var files = Directory.EnumerateFiles(eventDirPath).ToList();
            var file = files[new Random().Next(files.Count())];

            var script = CSharpScript.Create(File.ReadAllText(Path.Combine(file)), ScriptOptions.Default, typeof(AdvData));
            var data = new AdvData
            {
                info = info,
                state = new AdvState()
            }; 

            script = script.ContinueWith("Event000();");
            script.RunAsync(data).Wait();
            Console.WriteLine(data.state.CoolDown);

            info = data.info;
            return data.state;
        }
    }

    public class AdvData
    {
        public UserInfo info;
        public AdvState state;

        public string WeightRandom(List<KeyValuePair<string, int>> events)
        {
            int total = events.Sum(x => x.Value);
            int random = new Random().Next(0, total);

            int sum = 0;
            foreach (var value in events)
            {
                sum += value.Value;
                if (sum >= random)
                    return value.Key;
            }
            return events.First().Key;
        }

        public bool HasFlag(string flag)
        {
            return info.flags.Split(';').Contains(flag);
        }

        public void AddFlag(string flag)
        {
            var flags = info.flags.Split(';').ToList();
            if (!flags.Contains(flag))
                flags.Append(flag);
            info.flags = String.Join(';', flags);
        }

        public void RemoveFlag(string flag)
        {
            var flags = info.flags.Split(';').ToList();
            if (flags.Contains(flag))
                flags.Remove(flag);
            info.flags = String.Join(';', flags);
        }
    }

    public class AdvState
    {
        public DateTime CoolDown;
        public string Content;
        public string NextEventID;
    }
}
