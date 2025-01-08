namespace Client.Utils
{
    public class Session
    {
        public static string UserId { get; set; }
        public static string Username { get; set; }

        private static Session _instance;
        public static Session Instance => _instance ??= new Session();

        private Session() { }

        public static void ClearSession()
        {
            UserId = string.Empty;
            Username = string.Empty;
        }
    }
}
