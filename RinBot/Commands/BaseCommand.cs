namespace RinBot.Commands
{
    abstract internal class BaseCommand
    {
        public virtual void OnInit() { }
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
    }
}
