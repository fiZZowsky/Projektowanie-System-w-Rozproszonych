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

        MulticastService multicastService = new MulticastService();
        multicastService.AnnouncePresence(port);

        _ = Task.Run(() => multicastService.ListenForServersAsync(
            discoveredPort => Console.WriteLine($"[Discovery] Found server on port {discoveredPort}."),
            shutdownPort => Console.WriteLine($"[Shutdown] Server on port {shutdownPort} has shut down.")
        ));

        ServerManager serverManager = new ServerManager(port);
        try
        {
            serverManager.Start();
        }
        finally
        {
            multicastService.AnnounceShutdown(port);
        }
    }
}