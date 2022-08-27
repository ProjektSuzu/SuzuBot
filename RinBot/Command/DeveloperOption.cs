using RinBot.Core.Components;
using RinBot.Core;
using RinBot.Core.Components.Attributes;
using RinBot.Core.Components.Managers;
using RinBot.Core.KonataCore.Events;
using System.Text;

namespace RinBot.Command
{
    [Module("开发者选项", "AkulaKirov.DeveloperOption", true)]
    internal class DeveloperOption
    {
        [TextCommand("模块重载", "reload", UserPermission.Root)]
        public void OnReload(MessageEventArgs messageEvent)
        {
            GlobalScope.CommandManager.ReloadModules();
            messageEvent.Reply($"[CMD]\n载入了 {CommandManager.Instance.ModuleCount} 个模块, {CommandManager.Instance.CommandCount} 个命令.");
        }

        [TextCommand("查看日志", "log", UserPermission.Root)]
        public void OnPeekLog(MessageEventArgs messageEvent)
        {
            string path = Path.Combine(GlobalScope.ROOT_DIR_PATH, "log", "log.txt");
            FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader streamReader = new(fs, Encoding.UTF8);
            var log = streamReader.ReadToEnd()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(20);
            messageEvent.Reply(string.Join(Environment.NewLine, log));
        }
    }
}
