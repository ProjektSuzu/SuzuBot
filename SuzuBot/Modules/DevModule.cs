using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Newtonsoft.Json;
using SuzuBot.Common.Attributes;
using SuzuBot.Common;
using SuzuBot.Common.EventArgs.Messages;
using SuzuBot.Core.Databases.Tables;
using SuzuBot.Utils;

namespace SuzuBot.Modules;

[Module("开发者工具")]
internal class DevModule : BaseModule
{
    [Command("状态汇报", "status", Priority = 0)]
    public Task BotStatusReport(MessageEventArgs eventArgs)
    {
        StringBuilder builder = new("[SuzuBot]\n");
        builder.AppendLine($"SuzuBot-{SuzuBotBuildStamp.Version}");
        builder.AppendLine($"Branch: {SuzuBotBuildStamp.Branch}@{SuzuBotBuildStamp.CommitHash[..8]}");
        builder.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"Architecture: {RuntimeInformation.RuntimeIdentifier} {Environment.ProcessorCount} Thread(s)");
        builder.AppendLine($"Memory: {Environment.WorkingSet / 1000000} MB");
        builder.AppendLine($"UpTime: {DateTime.Now - Process.GetCurrentProcess().StartTime:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}");
        builder.AppendLine();
        builder.AppendLine($"Modules/Commands: {Context.ModuleManager.Modules.Count}/{Context.ModuleManager.Commands.Count}");
        builder.AppendLine($"ExecuteCount: {Context.ModuleManager.ExecuteCount}");
        builder.AppendLine($"ExceptionCount: {Context.ModuleManager.ExceptionCount}");
        if (Context.ModuleManager.LastException is not null)
        {
            builder.AppendLine($"LastException: {Context.ModuleManager.LastException.GetType().Name}");
        }
        builder.AppendLine();
        builder.AppendLine($"LastCommandCostMillisecond: {Context.ModuleManager.LastCommandCostMillisecond} ms");
        builder.AppendLine();
        builder.AppendLine("[Konata.Core]");
        builder.AppendLine($"Konata.Core-{KonataBuildStamp.Version}");
        builder.AppendLine($"Branch: {KonataBuildStamp.Branch}@{KonataBuildStamp.CommitHash[..8]}");
        builder.AppendLine($"Groups/Friends: {eventArgs.Bot.GetGroupList().Result.Count}/{eventArgs.Bot.GetFriendList().Result.Count}");
        builder.AppendLine(DateTime.UtcNow.ToString("O"));

        return eventArgs.Reply(builder.ToString());
    }
    [Command("历史错误", "exception", "error", Priority = 0, AuthGroup = "admin", AuthFailWarning = true)]
    public async Task LastExceptions(MessageEventArgs eventArgs)
    {
        StringBuilder builder = new StringBuilder("[Exceptions]\n");
        var exceptions = await Context.DataBaseManager.Connection
            .Table<ExceptionRecord>()
            .OrderByDescending(x => x.Date)
            .Take(7).ToArrayAsync();
        if (!exceptions.Any())
        {
            builder.AppendLine("No Exceptions");
            await eventArgs.Reply(builder.ToString());
            return;
        }

        foreach (var ex in exceptions)
        {
            builder.AppendLine($"{ex.Date:g} {ex.Type}\n{ex.Message}\n");
        }

        await eventArgs.Reply(builder.ToString());
    }
    [Command("内存回收", "gc", Priority = 0, AuthGroup = "admin", AuthFailWarning = true)]
    public Task GarbageCollect(MessageEventArgs eventArgs)
    {
        var beforeWorkingSet = Environment.WorkingSet;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var afterWorkingSet = Environment.WorkingSet;

        return eventArgs.Reply(new MessageBuilder($"[GC]\nCollected\n{beforeWorkingSet / 1000000} MB => {afterWorkingSet / 1000000} MB"));
    }
    [Command("模块重载", "reload", Priority = 0, AuthGroup = "admin", AuthFailWarning = true)]
    public async Task ModuleReload(MessageEventArgs eventArgs)
    {
        Context.ModuleManager.ClearModules();
        Context.ModuleManager.RegisterModule(Assembly.GetExecutingAssembly());
        await eventArgs.Reply(new MessageBuilder("[Reload]\nReloaded"));
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
