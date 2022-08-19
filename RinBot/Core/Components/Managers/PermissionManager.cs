namespace RinBot.Core.Components.Managers
{
    public enum UserLevel : short
    {
        User,
        GroupAdmin,
        Admin,
        Root,
    }

    internal class PermissionManager
    {
        #region Singleton
        public static PermissionManager Instance = new Lazy<PermissionManager>(() => new PermissionManager()).Value;
        private PermissionManager()
        {

        }
        #endregion

        public void OnInit()
        {

        }
    }
}
