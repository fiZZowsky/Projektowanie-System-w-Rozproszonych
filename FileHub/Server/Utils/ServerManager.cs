using Grpc.Core;
using Server.Services;

namespace Server.Utils;

public class ServerManager
{
    private readonly int _port;

    public ServerManager(int port)
    {
        _port = port;
    }

    public void Start()
    {
        var server = new Grpc.Core.Server
        {
            Services = { Common.GRPC.DistributedFileServer.BindService(new ServerService()) },
            Ports = { new ServerPort("localhost", _port, ServerCredentials.Insecure) }
        };

        Console.WriteLine($"[Server] Starting on port {_port}...");
        server.Start();

        Console.WriteLine("[Server] Press ENTER to stop the server.");
        Console.ReadLine();

        server.ShutdownAsync().Wait();
        Console.WriteLine("[Server] Shutdown completed.");
    }
}
