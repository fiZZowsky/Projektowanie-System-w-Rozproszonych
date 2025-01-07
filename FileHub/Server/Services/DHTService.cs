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
        private readonly string _storageDirectory;

        public DHTService(int port)
        {
            _port = port;
        }

        public async void AddNode(int port)
        {
            var nodeHash = DHTManager.ComputeHash(port.ToString());
            if (!_nodes.Any(n => n.Port == port))
            {
                var newNode = new DHTNode { Address = "localhost", Port = port, Hash = nodeHash };
                _nodes.Add(newNode);
                _nodes.Sort((a, b) => a.Hash.CompareTo(b.Hash));
                Console.WriteLine($"[DHT] Added node: {port} (Hash: {nodeHash}).");
                RecalculateResponsibilities();
                await RebalanceFiles();
            }
        }

        public async void RemoveNode(int port, string storageDirectoryPath = null)
        {
            var node = _nodes.FirstOrDefault(n => n.Port == port);
            if (node != null)
            {
                _nodes.Remove(node);
                Console.WriteLine($"[DHT] Removed node: {port}.");
                RecalculateResponsibilities();
                await RebalanceFiles(node);

                if(storageDirectoryPath != null)
                {
                    var files = Directory.GetFiles(storageDirectoryPath).ToList();

                    if (files.Any())
                    {
                        foreach (var filePath in files)
                        {
                            try
                            {
                                File.Delete(filePath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[FilesService] Error deleting file {filePath}: {ex.Message}");
                            }
                        }
                        var directoryInfo = new DirectoryInfo(storageDirectoryPath);
                        if (directoryInfo.Exists && !directoryInfo.GetFiles().Any() && !directoryInfo.GetDirectories().Any())
                        {
                            try
                            {
                                Directory.Delete(storageDirectoryPath);
                                Console.WriteLine($"[FilesService] Directory {storageDirectoryPath} deleted successfully.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[FilesService] Error deleting directory {storageDirectoryPath}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private void RecalculateResponsibilities()
        {
            if (!_nodes.Any())
            {
                Console.WriteLine("[DHT] No nodes available.");
                return;
            }

            Console.WriteLine("[DHT] Recalculating responsibilities...");

            var sortedNodes = _nodes.OrderBy(n => n.Hash).ToList();
            for (int i = 0; i < sortedNodes.Count; i++)
            {
                var currentNode = sortedNodes[i];
                var nextNode = sortedNodes[(i + 1) % sortedNodes.Count];  // Zawijanie na początek

                // Ustalanie zakresu odpowiedzialności węzła
                currentNode.ResponsibleRangeStart = currentNode.Hash;
                currentNode.ResponsibleRangeEnd = (nextNode.Hash > currentNode.Hash)
                                                  ? nextNode.Hash - 1
                                                  : nextNode.Hash;

                Console.WriteLine($"[DHT] Node {currentNode.Port} responsible for range {currentNode.ResponsibleRangeStart} - {currentNode.ResponsibleRangeEnd}");
            }
        }

        private async Task RebalanceFiles(DHTNode removedNode = null)
        {
            Console.WriteLine("[DHT] Rebalancing files...");

            List<bool> rebalancingResults = new List<bool>();

            if (removedNode != null)
            {
                Console.WriteLine($"[DHT] Redistributing files from removed node: {removedNode.Port}");
                var successorNode = _nodes.OrderBy(n => n.Hash).FirstOrDefault(n => n.Hash > removedNode.Hash)
                                    ?? _nodes.OrderBy(n => n.Hash).First();

                foreach (var file in _fileToNodeMap.Keys.Where(f => _fileToNodeMap[f] == removedNode.Address).ToList())
                {
                    _fileToNodeMap[file] = successorNode.Address;
                    bool result = await TransferFile(file, removedNode, successorNode);
                    rebalancingResults.Add(result);
                }
            }

            foreach (var file in _fileToNodeMap.Keys)
            {
                var newResponsibleNode = FindResponsibleNode(file);
                if (_fileToNodeMap[file] != newResponsibleNode.Address)
                {
                    var currentNode = _nodes.FirstOrDefault(n => n.Address == _fileToNodeMap[file]);
                    _fileToNodeMap[file] = newResponsibleNode.Address;
                    if (currentNode != null)
                    {
                        bool result = await TransferFile(file, currentNode, newResponsibleNode);
                        rebalancingResults.Add(result);
                    }
                }
            }

            if (rebalancingResults.All(r => r == true))
            {
                Console.WriteLine("[DHT] All files have been successfully rebalanced.");
            }
            else
            {
                Console.WriteLine("[DHT] Some files could not be rebalanced.");
            }
        }

        private async Task<bool> TransferFile(string fileName, DHTNode sourceNode, DHTNode targetNode)
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

                        var sourceFilePath = Path.Combine(_storageDirectory, fileName);
                        if (File.Exists(sourceFilePath))
                        {
                            File.Delete(sourceFilePath);
                            Console.WriteLine($"[DHT] File {fileName} deleted from source node {sourceNode.Port}.");
                        }
                        else
                        {
                            Console.WriteLine($"[DHT] File {fileName} not found on source node {sourceNode.Port}.");
                        }

                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[DHT] Error transferring file: {transferResponse.Message}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"[DHT] Error downloading file from source node {sourceNode.Port}: {fileContentResponse.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DHT] Error during file transfer: {ex.Message}");
                return false;
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
