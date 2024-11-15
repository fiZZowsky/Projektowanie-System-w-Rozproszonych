namespace Server.Networking
{
    public class DHT
    {
        private readonly List<Server.Core.Server> _servers;
        private readonly int _keyRange;

        public DHT(int keyRange)
        {
            _keyRange = keyRange;
            _servers = new List<Server.Core.Server>();
        }

        public void AddServer(Server.Core.Server newServer)
        {
            _servers.Add(newServer);
            RebalanceData();
        }

        public void RemoveServer(Server.Core.Server server)
        { 
            _servers.Remove(server);
            RebalanceData();
        }

        public Server.Core.Server GetServerForKey(int key)
        {
            var serverIndex = key % _servers.Count;
            return _servers[serverIndex];
        }

        private void RebalanceData()
        {
            Console.WriteLine("Rebalansowanie danych po zmianach...");
            int keyRangeSize = _keyRange / _servers.Count;
            for (int i = 0; i < _servers.Count; i++)
            {
                var server = _servers[i];
                server.KeyRangeStart = i * keyRangeSize;
                server.KeyRangeEnd = (i + 1) * keyRangeSize - 1;

                if (i == _servers.Count - 1)
                {
                    server.KeyRangeEnd = _keyRange - 1;
                }
            }
        }

        public void DisplayDHTState()
        {
            Console.WriteLine("Aktualny stan DHT:");
            foreach (var server in _servers)
            {
                Console.WriteLine($"Serwer {server.GetServerId()} - zakres kluczy: {server.KeyRangeStart} do {server.KeyRangeEnd}");
            }
        }
    }
}
