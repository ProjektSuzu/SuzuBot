using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Core.Tables;
using SuzuBot.Utils;

namespace SuzuBot.Modules.Basic;
public class DebugModule : BaseModule
{
    public DebugModule()
    {
        Name = "调试模块";
        IsCritical = true;
    }

    [Command("状态报告", "^status$", Priority = 0)]
    public Task BotStatus(MessageEventArgs eventArgs, string[] args)
    {
        StringBuilder builder = new("[SuzuBot]\n");
        builder.AppendLine($"SuzuBot-{SuzuBotBuildStamp.Version}_{SuzuBotBuildStamp.Branch}@{SuzuBotBuildStamp.CommitHash[..8]}");
        builder.AppendLine($"Konata.Core-{KonataBuildStamp.Version}_{KonataBuildStamp.Branch}@{KonataBuildStamp.CommitHash[..8]}\n");
        builder.AppendLine($"运行时版本: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"主机架构: {RuntimeInformation.RuntimeIdentifier} {Environment.ProcessorCount} Thread(s)");
        builder.AppendLine($"内存占用: {Environment.WorkingSet / 1000000} MB");
        builder.AppendLine($"运行时间: {DateTime.Now - Process.GetCurrentProcess().StartTime:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}\n");

        builder.AppendLine($"模块/命令: {Context.ModuleManager.Modules.Count}/{Context.ModuleManager.Commands.Count}");
        builder.AppendLine($"当前命令执行计数器: {Context.ModuleManager.ExecuteCount}");
        builder.AppendLine($"总计命令执行计数器: {Context.DatabaseManager.Connection.Table<ExecutionRecord>().CountAsync().Result}");
        builder.AppendLine($"错误计数器: {Context.ModuleManager.ExceptionCount}");

        builder.AppendLine($"上一次命令执行用时: {Context.ModuleManager.LastCommandCostMillisecond} ms\n");

        builder.Append(DateTime.UtcNow.ToString("O"));

        return eventArgs.Reply(builder.ToString());
    }

    [Command("模块重载", "^reload$", Priority = 0, AuthGroup = AuthGroup.Root, WarnOnAuthFail = true)]
    public Task ReloadModules(MessageEventArgs eventArgs, string[] args)
    {
        Context.ModuleManager.ReloadModules();
        return eventArgs.Reply("模块已重载\n" +
            $"模块/命令: {Context.ModuleManager.Modules.Count}/{Context.ModuleManager.Commands.Count}");
    }
}