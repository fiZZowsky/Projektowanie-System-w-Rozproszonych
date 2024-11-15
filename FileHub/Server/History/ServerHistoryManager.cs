using Server.Info;

namespace Server.History
{
    public static class ServerHistoryManager
    {
        public static async Task<string> GetNextServerIdAsync(string filePath)
        {
            List<ServerInfo> serverHistory = await LoadServerHistoryAsync(filePath);
            int nextServerNumber = serverHistory.Count + 1;
            return $"serwer {nextServerNumber}";
        }

        public static async Task<List<ServerInfo>> LoadServerHistoryAsync(string filePath)
        {
            var serverHistory = new List<ServerInfo>();

            if (File.Exists(filePath))
            {
                string[] lines = await File.ReadAllLinesAsync(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(';');
                    if (parts.Length == 2)
                    {
                        string id = parts[0].Trim();
                        int port = int.Parse(parts[1].Trim());
                        serverHistory.Add(new ServerInfo(id, port));
                    }
                }
            }

            return serverHistory;
        }

        public static async Task LogServerStart(string filePath, string serverId, int port)
        {
            var serverInfo = $"{serverId}; {port}";
            await File.AppendAllTextAsync(filePath, serverInfo + Environment.NewLine);
        }
    }
}
