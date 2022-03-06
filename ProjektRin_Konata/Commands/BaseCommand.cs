namespace ProjektRin.Commands
{
    public abstract class BaseCommand
    {
        private bool _isEnabled;
        public bool IsEnabled => _isEnabled;
        public abstract string Help { get; }
        public abstract void OnInit();
        public virtual void OnEnable() => _isEnabled = true;
        public virtual void OnDisable() => _isEnabled = false;
    }
}
