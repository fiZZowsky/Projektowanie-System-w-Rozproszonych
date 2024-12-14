using Server.Services;
using Server.Utils;
using Server;

class Program
{
    static async Task Main(string[] args)
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

        List<string> userPaths = AppConfig.UserPaths;

        foreach (string path in userPaths)
        {
            Console.WriteLine($"[Startup] Initializing file watcher for path: {path}");
            var folderWatcher = new WatcherService(path);
            folderWatcher.StartWatching();
        }

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
            }
        ));

        ServerManager serverManager = new ServerManager(AppConfig.DefaultFilesStoragePath, port);
        try
        {
            dhtService.AddNode(port); // Dodaj lokalny serwer do DHT
            serverManager.Start();
        }
        finally
        {
            multicastService.AnnounceShutdown(port);
            dhtService.RemoveNode(port); // Usuń lokalny serwer z DHT
        }
    }
}
