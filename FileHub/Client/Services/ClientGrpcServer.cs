using Grpc.Core;

namespace Client.Services;

public class ClientGrpcServer
{
    private readonly ClientService _clientService;
    private readonly MetadataHandler _metadataHandler;
    private WatcherService _folderWatcher;
    private Grpc.Core.Server? _grpcServer;

    public ClientGrpcServer(ClientService clientService, MetadataHandler metadataHandler)
    {
        _clientService = clientService;
        _metadataHandler = metadataHandler;
        _grpcServer = null;
    }

    public void SetFolderWatcher(WatcherService folderWatcher)
    {
        _folderWatcher = folderWatcher;
    }

    public void StartGrpcServer()
    {
        var clientIp = _metadataHandler.GetComputerIp();
        var clientPort = _metadataHandler.GetAvailablePort();
        var _clientServerService = new ClientServerService(_clientService, _folderWatcher);

        _grpcServer = new Grpc.Core.Server
        {
            Services = { Common.GRPC.DistributedFileServer.BindService(_clientServerService) },
            Ports = { new ServerPort(clientIp, clientPort, ServerCredentials.Insecure) }
        };

        _grpcServer.Start();
        Console.WriteLine("gRPC Server started.");
    }

    public async Task StopGrpcServer()
    {
        if (_grpcServer != null)
        {
            await _grpcServer.ShutdownAsync();
            _grpcServer = null;
            Console.WriteLine("gRPC Server stopped.");
        }
        else
        {
            Console.WriteLine("gRPC Server is not running.");
        }
    }
}
