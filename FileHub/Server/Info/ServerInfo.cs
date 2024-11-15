namespace Server.Info
{
    public class ServerInfo
    {
        public string Id { get; }
        public int Port { get; }

        public ServerInfo(string id, int port)
        {
            Id = id;
            Port = port;
        }
    }
}
