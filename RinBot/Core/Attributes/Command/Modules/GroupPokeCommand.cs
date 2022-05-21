using RinBot.Core.Components;

namespace RinBot.Core.Attributes.Command.Modules
{
    internal class GroupPokeCommand : Command
    {
        public GroupPokeCommand(string name, Permission permission = Permission.User) : base(name, permission)
        {
        }
    }
}
