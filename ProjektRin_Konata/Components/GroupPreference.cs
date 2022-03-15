namespace ProjektRin.Components
{
    internal class GroupPreference
    {
        public uint GroupUin;
        public bool PassiveMode = false;

        public List<string> DisabledCommandSets = new();

        public GroupPreference(uint groupUin) => GroupUin = groupUin;
    }
}
