using Common;
using Common.Converters;
using Common.GRPC;
using Common.Models;
using Google.Protobuf;
using Grpc.Net.Client;
using Server.Utils;
using System.IO;
using System.Text.Json;

namespace Server.Services
{
    public class DHTService
    {
        private readonly int _port;
        private readonly MulticastService _multicastService;
        private readonly List<DHTNode> _nodes = new List<DHTNode>();
        private string _storageDirectory;

        public DHTService(int port, MulticastService multicastService)
        {
            _port = port;
            _multicastService = multicastService;
        }

        public async Task AddNode(int port)
        {
            if (!_nodes.Any(n => n.Port == port))
            {
                var nodeHash = DHTManager.ComputeHash(port.ToString());
                var newNode = new DHTNode { Address = AppSettings.DefaultAddress, Port = port, Hash = nodeHash };
                _nodes.Add(newNode);
                _nodes.Sort((a, b) => a.Hash.CompareTo(b.Hash));
                Console.WriteLine($"[DHT] Added node: {port} (Hash: {nodeHash}).");

                RecalculateResponsibilities();
                await RebalanceFiles();
            }
        }

        public async void AddDiscoveredNode(int port)
        {
            await AddNode(port);

            // Broadcast updated list to new node
            var serverListJson = JsonSerializer.Serialize(_nodes.Select(n => n.Port));
            await _multicastService.AnnounceNodesList(port, serverListJson);
        }

        public async void UpdateNodesList(List<int> ports)
        {
            foreach (var port in ports)
            {
                await AddNode(port);
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

                if (storageDirectoryPath != null)
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

        public async Task RebalanceFiles(DHTNode removedNode = null)
        {
            Console.WriteLine("[DHT] Rebalancing files...");

            List<bool> rebalancingResults = new List<bool>();

            if (removedNode != null)
            {
                if(_port == AppConfig.StartPort)
                {
                    Console.WriteLine($"[DHT] Redistributing files from removed node: {removedNode.Port}");

                    string[] directories = Directory.GetDirectories(AppConfig.DefaultFilesStoragePath, $"{removedNode.Port}*");

                    // Przenosimy pliki z usuniętego węzła na inne węzły
                    foreach (var directory in directories)
                    {
                        if (Path.GetFileName(directory).StartsWith(removedNode.Port.ToString()))
                        {
                            string[] files = Directory.GetFiles(directory);
                            foreach (var file in files)
                            {
                                var successorNode = DHTManager.FindResponsibleNode(file, _nodes);

                                if (File.Exists(file))
                                {
                                    // Sprawdzamy, czy plik jest w użyciu
                                    bool fileLocked = true;
                                    DateTime startTime = DateTime.Now;

                                    while (fileLocked && (DateTime.Now - startTime).TotalMinutes < 1)
                                    {
                                        try
                                        {
                                            // Próbujemy otworzyć plik do odczytu z blokadą
                                            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                                            {
                                                fileLocked = false; // Plik jest dostępny
                                            }
                                        }
                                        catch (IOException)
                                        {
                                            // Jeśli plik jest w użyciu przez inny proces, ponownie spróbujemy
                                            await Task.Delay(1000);
                                        }
                                    }

                                    if (fileLocked)
                                    {
                                        rebalancingResults.Add(false);
                                        Console.WriteLine("[DHT] File is currently in use and could not be accessed within the time limit.");
                                    }

                                    // Jeśli plik jest dostępny, odczytujemy jego zawartość
                                    var fileContent = await File.ReadAllBytesAsync(file);
                                    var fileInfo = new FileInfo(file);
                                    var decodedFile = await FormatConverter.DecodeFileDataFromName(fileInfo);
                                    var fileData = new FileData
                                    {
                                        FileName = decodedFile.FileName,
                                        FileContent = ByteString.CopyFrom(fileContent),
                                        FileType = decodedFile.FileType,
                                        CreationDate = decodedFile.CreationDate,
                                        UserId = decodedFile.UserId,
                                    };

                                    bool result = await TransferFile(fileData, file, removedNode, successorNode);
                                    rebalancingResults.Add(result);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (_storageDirectory != null)
                {
                    string[] files = Directory.GetFiles(_storageDirectory);
                    foreach (var file in files)
                    {
                        var successorNode = DHTManager.FindResponsibleNode(file, _nodes);
                        if (successorNode.Port != _port)
                        {
                            var currentNode = _nodes.First(x => x.Port == _port);
                            var relativeFilePath = file.Replace($"{_storageDirectory}\\", string.Empty);
                            bool result = await TransferFile(relativeFilePath, currentNode, successorNode);
                            rebalancingResults.Add(result);
                        }
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
                var sourceChannel = GrpcChannel.ForAddress($"http://{AppSettings.DefaultAddress}:{sourceNode.Port}");
                var targetChannel = GrpcChannel.ForAddress($"http://{AppSettings.DefaultAddress}:{targetNode.Port}");

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

        private async Task<bool> TransferFile(FileData file, string fullFilePath, DHTNode sourceNode, DHTNode targetNode)
        {
            Console.WriteLine($"[DHT] Transferring file {file.FileName} from {sourceNode.Port} to {targetNode.Port}...");

            try
            {
                // Stworzenie kanału gRPC dla docelowego węzła
                var targetChannel = GrpcChannel.ForAddress($"http://{AppSettings.DefaultAddress}:{targetNode.Port}");

                var targetClient = new DistributedFileServer.DistributedFileServerClient(targetChannel);

                // Przekazujemy plik na nowy serwer
                var transferResponse = await targetClient.TransferFileAsync(new TransferRequest
                {
                    FileName = file.FileName,
                    FileContent = file.FileContent,
                    FileType = file.FileType,
                    CreationDate = file.CreationDate,
                    UserId = file.UserId
                });
                if (transferResponse.Success)
                {
                    Console.WriteLine($"[DHT] File {file.FileName} transferred successfully from {sourceNode.Port} to {targetNode.Port}.");

                    if (File.Exists(fullFilePath))
                    {
                        File.Delete(fullFilePath);
                        Console.WriteLine($"[DHT] File {file.FileName} deleted from source node {sourceNode.Port}.");
                    }
                    else
                    {
                        Console.WriteLine($"[DHT] File {file.FileName} not found on source node {sourceNode.Port}.");
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine($"[DHT] Error transferring file: {transferResponse.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DHT] Error during file transfer: {ex.Message}");
                return false;
            }
        }

        public List<DHTNode> GetNodes() => _nodes;
        public int GetServerPort() => _port;
        public void SetStorageDirectory(string dir) => _storageDirectory = dir;
    }
}
