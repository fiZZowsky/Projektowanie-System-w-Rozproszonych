using Server.Services;
using Server.Utils;
using Server;

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

            var multicastService = new MulticastService();
            var dhtService = new DHTService(port);

            multicastService.AnnouncePresence(port);

            ServerManager serverManager = new ServerManager(AppConfig.DefaultFilesStoragePath, port, dhtService);

            _ = Task.Run(() => multicastService.ListenForServersAsync(
                discoveredPort =>
                {
                    Console.WriteLine($"[Discovery] Found server on port {discoveredPort}.");
                    dhtService.AddNode(discoveredPort);
                },
                shutdownPort =>
                {
                    Console.WriteLine($"[Shutdown] Server on port {shutdownPort} has shut down.");
                    dhtService.RemoveNode(shutdownPort);
                },
                clientsList =>
                {
                    Console.WriteLine("[Clients] Received updated client list.");
                    serverManager.UpdateClientsList(clientsList);
                }
            ));

            try
            {
                dhtService.AddNode(port); // Dodaj lokalny serwer do DHT
                serverManager.Start();
            }
            finally
            {
                multicastService.AnnounceShutdown(port);
                dhtService.RemoveNode(port, serverManager.GetStorageDirectoryPath()); // Usuń lokalny serwer z DHT
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while running the server application\n{ex.Message}");
        }
    }
}