using Common;
using Grpc.Core;
using Server.Services;

namespace Server.Utils;

public class ServerManager
{
    private readonly int _port;
    private readonly DHTService _dhtService;
    private string _storageDirectory;
    private ServerService? _serverService;

    public ServerManager(string defaultStoragePath, int port, DHTService dhtService)
    {
        _storageDirectory = defaultStoragePath;
        _port = port;
        _dhtService = dhtService;
    }

    public void Start()
    {
        var uniqueId = Guid.NewGuid().ToString();
        var serverPrefix = $"{_port}";
        var serverDirectories = Directory.GetDirectories(_storageDirectory, $"{serverPrefix}*");

        if (serverDirectories.Length == 0)
        {
            _storageDirectory = Path.Combine(_storageDirectory, $"{serverPrefix}_{uniqueId}");
            Directory.CreateDirectory(_storageDirectory);
            Console.WriteLine($"[Server {_port}] Created storage directory: {_storageDirectory}");
        }
        else
        {
            _storageDirectory = serverDirectories[0];
            Console.WriteLine($"[Server {_port}] Using existing storage directory: {_storageDirectory}");
        }

        _serverService = new ServerService(_storageDirectory, _dhtService);

        var server = new Grpc.Core.Server
        {
            Services = { Common.GRPC.DistributedFileServer.BindService(_serverService) },
            Ports = { new ServerPort(AppSettings.DefaultAddress, _port, ServerCredentials.Insecure) }
        };

        Console.WriteLine($"[Server] Starting on port {_port}...");
        server.Start();

        Console.WriteLine("[Server] Press ENTER to stop the server.");
        Console.ReadLine();

        server.ShutdownAsync().Wait();
        Console.WriteLine("[Server] Shutdown completed.");
    }

    public string GetStorageDirectoryPath()
    {
        return _storageDirectory;
    }

    public void UpdateClientsList(string clientsList)
    {
        if (_serverService == null)
        {
            Console.Write("[Server] ServerService is not initialized. Start the server first.");
            return;
        }

        _serverService.UpdateClientsList(clientsList);
    }
}