using Server.History;
using Server.Networking;

namespace Server.Core
{
    class Program
    {
        private static string serverHistoryFilePath = "server_history.txt";

        static async Task Main(string[] args)
        {
            var portManager = new PortManager(5000, 6000);

            string serverId = await ServerHistoryManager.GetNextServerIdAsync(serverHistoryFilePath);

            int? port = portManager.AssignPort();
            if (port == null)
            {
                Console.WriteLine("Brak dostępnych portów!");
                return;
            }

            var cancellationTokenSource = new CancellationTokenSource();

            int keyRangeStart = 0;
            int keyRangeEnd = 9999;
            var server = new Server(serverId, port.Value, keyRangeStart, keyRangeEnd);

            var dht = new DHT(10000);
            dht.AddServer(server);

            var serverTask = server.StartAsync(cancellationTokenSource.Token);

            Console.WriteLine($"Serwer {serverId} uruchomiony na porcie {port}");

            dht.DisplayDHTState();

            Console.WriteLine("Naciśnij Enter, aby zatrzymać serwer...");
            Console.ReadLine();

            cancellationTokenSource.Cancel();
            await serverTask;
            server.Stop();

            portManager.ReleasePort(port.Value);
            Console.WriteLine($"Port {port} zwrócony do puli.");

            await ServerHistoryManager.LogServerStart(serverHistoryFilePath, serverId, port.Value);
        }
    }
}
