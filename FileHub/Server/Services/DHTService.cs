using Common.GRPC;
using Common.Models;
using Grpc.Net.Client;
using Server.Utils;

namespace Server.Services
{
    public class DHTService
    {
        private readonly int _port;
        private readonly List<DHTNode> _nodes = new List<DHTNode>();
        private readonly Dictionary<string, string> _fileToNodeMap = new Dictionary<string, string>(); // Mapowanie plików na serwery

        public DHTService(int port)
        {
            _port = port;
        }

        public void AddNode(int port)
        {
            var nodeHash = DHTManager.ComputeHash(port.ToString());
            if (!_nodes.Any(n => n.Port == port))
            {
                _nodes.Add(new DHTNode { Address = "localhost", Port = port, Hash = nodeHash });
                _nodes.Sort((a, b) => a.Hash.CompareTo(b.Hash));
                Console.WriteLine($"[DHT] Added node: {port} (Hash: {nodeHash}).");
                RecalculateResponsibilities();
            }
        }

        public void RemoveNode(int port)
        {
            var node = _nodes.FirstOrDefault(n => n.Port == port);
            if (node != null)
            {
                _nodes.Remove(node);
                Console.WriteLine($"[DHT] Removed node: {port}.");
                RecalculateResponsibilities(node); // Przekazujemy usunięty węzeł
            }
        }

        private void RecalculateResponsibilities(DHTNode removedNode = null)
        {
            Console.WriteLine("[DHT] Recalculating responsibilities...");
            foreach (var node in _nodes)
            {
                Console.WriteLine($"Node {node.Port}: Responsible for hash range...");
                // Możesz dodać bardziej szczegółową logikę dla zakresów odpowiedzialności
            }

            // Jeżeli węzeł został usunięty, wykonaj rebalance
            if (removedNode != null)
            {
                Console.WriteLine($"[DHT] Rebalancing files for removed node: {removedNode.Port}.");
                RebalanceFiles(removedNode);
            }
        }

        private void RebalanceFiles(DHTNode failedNode)
        {
            // Znajdź wszystkie pliki przypisane do usuniętego węzła
            var filesToReassign = _fileToNodeMap
                .Where(kv => kv.Value == failedNode.Address)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var file in filesToReassign)
            {
                // Znajdź nowy węzeł odpowiedzialny za plik
                var newResponsibleNode = FindResponsibleNode(file);
                _fileToNodeMap[file] = newResponsibleNode.Address;

                // Przenieś fizyczny plik na nowy serwer
                TransferFile(file, failedNode, newResponsibleNode);
            }
        }

        private async void TransferFile(string fileName, DHTNode sourceNode, DHTNode targetNode)
        {
            Console.WriteLine($"[DHT] Transferring file {fileName} from {sourceNode.Port} to {targetNode.Port}...");

            try
            {
                // Stworzenie kanału gRPC dla źródłowego i docelowego węzła
                var sourceChannel = GrpcChannel.ForAddress($"http://localhost:{sourceNode.Port}");
                var targetChannel = GrpcChannel.ForAddress($"http://localhost:{targetNode.Port}");

                var sourceClient = new DistributedFileServer.DistributedFileServerClient(sourceChannel);
                var targetClient = new DistributedFileServer.DistributedFileServerClient(targetChannel);

                // Pobieramy plik z serwera źródłowego
                var fileContentResponse = await sourceClient.DownloadFileByServerAsync(new DownloadByServerRequest { FileName = fileName });

                if (fileContentResponse.Success && fileContentResponse.Files.Count > 0)
                {
                    var fileData = fileContentResponse.Files[0];
                    // Przekazujemy plik na nowy serwer
                    var transferResponse = await targetClient.TransferFileAsync(new TransferRequest
                    {
                        FileName = fileName,
                        FileContent = fileData.FileContent,
                        FileType = fileData.FileType,
                        CreationDate = fileData.CreationDate,
                        UserId = fileData.UserId
                    });
                    if (transferResponse.Success)
                    {
                        Console.WriteLine($"[DHT] File {fileName} transferred successfully from {sourceNode.Port} to {targetNode.Port}.");
                    }
                    else
                    {
                        Console.WriteLine($"[DHT] Error transferring file: {transferResponse.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[DHT] Error downloading file from source node {sourceNode.Port}: {fileContentResponse.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DHT] Error during file transfer: {ex.Message}");
            }
        }

        public DHTNode FindResponsibleNode(string fileName)
        {
            var fileHash = DHTManager.ComputeHash(fileName);
            return _nodes.FirstOrDefault(n => n.Hash >= fileHash) ?? _nodes.First();
        }

        public List<DHTNode> GetNodes() => _nodes;
        public int GetServerPort() => _port;
    }
}
