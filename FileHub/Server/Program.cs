using Server.Services;
using Server.Utils;
using Server;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            int port = PortManager.FindAvailablePort(AppConfig.StartPort, AppConfig.EndPort);

            if (port == -1)
            {
                Console.WriteLine($"No available port in range {AppConfig.StartPort}-{AppConfig.EndPort}. Exiting.");
                return;
            }

            var multicastService = new MulticastService(port);
            var dhtService = new DHTService(port, multicastService);

            ServerManager serverManager = new ServerManager(AppConfig.DefaultFilesStoragePath, port, dhtService);

            _ = Task.Run(() => multicastService.ListenForServersAsync(
                async discoveredPort =>
                {
                    Console.WriteLine($"[Discovery] Found server on port {discoveredPort}.");
                    dhtService.AddDiscoveredNode(discoveredPort);
                },
                shutdownPort =>
                {
                    Console.WriteLine($"[Shutdown] Server on port {shutdownPort} has shut down.");
                    dhtService.RemoveNode(shutdownPort);
                },
                serversList =>
                {
                    var servers = JsonSerializer.Deserialize<List<int>>(serversList);
                    if (servers != null)
                    {
                        Console.WriteLine("[Multicast] Received updated server list.");
                        dhtService.UpdateNodesList(servers);
                    }
                },
                clientsList =>
                {
                    Console.WriteLine("[Clients] Received updated client list.");
                    serverManager.UpdateClientsList(clientsList);
                }
            ));

            try
            {
                await dhtService.AddNode(port); // Dodaj lokalny serwer do DHT
                multicastService.AnnouncePresence(port);
                serverManager.Start();
            }
            finally
            {
                multicastService.AnnounceShutdown(port);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while running the server application\n{ex.Message}");
        }
    }
}