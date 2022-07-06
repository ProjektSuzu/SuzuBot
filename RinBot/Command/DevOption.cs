using NLog;
using RinBot.BuildStamp;
using RinBot.Core.Component.Command;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Database;
using RinBot.Core.Component.ENV;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Permission;
using System.Text;

namespace RinBot.Command
{
    [Module("开发者选项", "org.akulak.devOption")]
    internal class DevOption
    {
        private Logger Logger = LogManager.GetLogger("DEV");

        [Command("环境变量", "env", MatchingType.StartsWith, ReplyType.Reply, UserRole.Admin)]
        public string OnEnv(RinEvent e, List<string> args)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("[Env]");
            if (args.Count == 0)
            {
                stringBuilder.AppendLine($"Count: {EnvManager.Instance.GetEnvCount()}");
                return stringBuilder.ToString();   
            }
            else if (args.Count < 1)
            {
                stringBuilder.AppendLine($"缺少参数 <funcName>");
                return stringBuilder.ToString();
            }
            else if (args.Count < 2)
            {
                stringBuilder.AppendLine($"缺少参数 <key>");
                return stringBuilder.ToString();
            }

            string funcName = args[0];
            string key = args[1];
            args = args.Skip(2).ToList();
            if (args == null)
                args = new();
            switch (funcName)
            {
                case "get":
                    {
                        if (!EnvManager.Instance.HasEnv(key))
                        {
                            stringBuilder.AppendLine($"变量不存在 {key}");
                        }
                        else
                        {
                            var values = EnvManager.Instance.GetEnv(key);
                            stringBuilder.AppendLine($"{key} =");
                            foreach (var value in values)
                            {
                                stringBuilder.AppendLine($"    {value};");
                            }
                        }
                        return stringBuilder.ToString();
                    }

                case "set":
                    {
                        if (!EnvManager.Instance.SetEnv(key, args))
                        {
                            stringBuilder.AppendLine($"设置变量时出错 {key}");
                            return stringBuilder.ToString();
                        }
                        var values = EnvManager.Instance.GetEnv(key);
                        stringBuilder.AppendLine($"{key} =");
                        foreach (var value in values)
                        {
                            stringBuilder.AppendLine($"    {value};");
                        }
                        return stringBuilder.ToString();
                    }

                case "add":
                    {
                        if (!EnvManager.Instance.HasEnv(key))
                        {
                            stringBuilder.AppendLine($"变量不存在 {key}");
                        }
                        else
                        {
                            var values = EnvManager.Instance.GetEnv(key);
                            values.AddRange(args);
                            if (!EnvManager.Instance.SetEnv(key, values))
                            {
                                stringBuilder.AppendLine($"设置变量时出错 {key}");
                                return stringBuilder.ToString();
                            }
                            values = EnvManager.Instance.GetEnv(key);
                            stringBuilder.AppendLine($"{key} =");
                            foreach (var value in values)
                            {
                                stringBuilder.AppendLine($"    {value};");
                            }
                        }
                        return stringBuilder.ToString();
                    }

                case "del":
                    {
                        if (!EnvManager.Instance.HasEnv(key))
                        {
                            stringBuilder.AppendLine($"变量不存在 {key}");
                        }
                        else
                        {
                            EnvManager.Instance.DelEnv(key);
                            stringBuilder.AppendLine($"变量已删除 {key}");
                        }
                        return stringBuilder.ToString();
                    }

                default:
                    stringBuilder.AppendLine($"找不到功能: {funcName}");
                    return stringBuilder.ToString();
            }
        }

        [Command("模块重载", "reload", MatchingType.StartsWith, ReplyType.Reply, UserRole.Admin)]
        public string OnReload(RinEvent e)
        {
            CommandManager.Instance.ClearCommands();
            CommandManager.Instance.RegisterCommands();
            return $"[CMD]\n载入了 {CommandManager.Instance.ModuleCount} 个模块, {CommandManager.Instance.CommandCount} 个命令.";
        }
        [Command("封禁", "ban", MatchingType.StartsWith, ReplyType.Reply, UserRole.Admin)]
        public string OnBan(RinEvent e, List<string> args)
        {
            if (args.Count <= 0)
                return "[DevOptions]\n缺少参数 <userId>";
            var userId = args[0];
            if (userId == e.SenderId)
                return "[DevOptions]\n不能封禁自己";
            if (e.EventSourceType == EventSourceType.QQ)
            {
                var uin = uint.Parse(userId);
                var info = PermissionManager.Instance.GetQQUserInfo(uin);
                info.UserRole = UserRole.Banned;
                PermissionManager.Instance.UpdateQQUserInfo(info);
                return $"[DevOptions]\n已封禁 {userId}";
            }
            else
            {
                return null;
            }
        }
        [Command("解封", "unban", MatchingType.StartsWith, ReplyType.Reply, UserRole.Admin)]
        public string OnUnBan(RinEvent e, List<string> args)
        {
            if (args.Count <= 0)
                return "[DevOptions]\n缺少参数 <userId>";
            var userId = args[0];
            if (userId == e.SenderId)
                return "[DevOptions]\n不能解封自己";
            if (e.EventSourceType == EventSourceType.QQ)
            {
                var uin = uint.Parse(userId);
                var info = PermissionManager.Instance.GetQQUserInfo(uin);
                info.UserRole = UserRole.User;
                PermissionManager.Instance.UpdateQQUserInfo(info);
                return $"[DevOptions]\n已解封 {userId}";
            }
            else
            {
                return null;
            }
        }
    }
}
