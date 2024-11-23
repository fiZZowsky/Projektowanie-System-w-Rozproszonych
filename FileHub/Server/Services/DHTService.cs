using Common.GRPC;
using Common.Models;
using Common.Utils;
using Grpc.Net.Client;

namespace Server.Services
{
    internal class DHTService
    {
        private readonly int _port = 5000;
        private readonly List<DHTNode> _nodes = new List<DHTNode>();

        public async Task DiscoverServersAsync()
        {
            for (int port = 5000; port <= 6000; port++)
            {
                if (port == _port) continue;

                try
                {
                    using var channel = GrpcChannel.ForAddress($"http://localhost:{port}");
                    var client = new DistributedFileServer.DistributedFileServerClient(channel);

                    var response = await client.PingAsync(new PingRequest());
                    if (response.Success && !_nodes.Any(n => n.Port == port))
                    {
                        var nodeHash = DHTManager.ComputeHash(port.ToString());
                        _nodes.Add(new DHTNode { Address = "localhost", Port = port, Hash = nodeHash });

                        Console.WriteLine($"[Serwer {_port}] Wykryto nowy serwer: {port} (Hash: {nodeHash}).");
                    }
                }
                catch
                {
                    // Serwer nie istnieje
                }
            }
        }
    }
}
