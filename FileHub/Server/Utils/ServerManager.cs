using Grpc.Core;
using Server.Services;

namespace Server.Utils;

public class ServerManager
{
    private readonly int _port;
    private string _storageDirectory;

    public ServerManager(string defaultStoragePath, int port)
    {
        _storageDirectory = defaultStoragePath;
        _port = port;
    }

    public void Start()
    {
        var uniqueId = Guid.NewGuid().ToString();
        _storageDirectory = Path.Combine(_storageDirectory, $"{_port}_{uniqueId}");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
            Console.WriteLine($"[Server {_port}] Created storage directory: {_storageDirectory}");
        }
        else
        {
            Console.WriteLine($"[Server {_port}] Storage directory already exists: {_storageDirectory}");
        }

        var server = new Grpc.Core.Server
        {
            Services = { Common.GRPC.DistributedFileServer.BindService(new ServerService(_storageDirectory)) },
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
