using Client.Utils;
using Common.Converters;
using Common.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using System.IO;
using static Common.GRPC.DistributedFileServer;

namespace Client.Services
{
    public class ClientService
    {
        private readonly string _serverAddress;
        private readonly MetadataHandler _metadataHandler;
        private AccountService _accountService;

        public ClientService(string serverAddress, MetadataHandler metadataHandler)
        {
            _serverAddress = serverAddress;
            _metadataHandler = metadataHandler;
            _accountService = new AccountService(_metadataHandler);
        }

        public async Task<List<Common.Models.NodeInfo>> GetAvailableServersAsync()
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            var client = new DistributedFileServerClient(channel);

            var response = await client.GetNodesAsync(new Empty());
            return ModelConverter.FromNodeListResponse(response);
        }

        private NodeInfo FindResponsibleServer(string fileName, List<NodeInfo> availableServers)
        {
            int hash = Math.Abs(fileName.GetHashCode());
            int serverIndex = hash % availableServers.Count; // Równomierne rozproszenie po serwerach
            return availableServers[serverIndex];
        }

        public async Task GetUserFilesAsync()
        {
            //var availableServers = await GetAvailableServersAsync();
            //var responsibleServer = FindResponsibleServer(_metadataHandler.GetComputerIp(), availableServers);

            //using var channel = GrpcChannel.ForAddress($"http://{responsibleServer.Address}:{responsibleServer.Port}");
            //var client = new DistributedFileServerClient(channel);

            //var request = new Common.GRPC.DownloadRequest
            //{
            //    UserId = Session.UserId,
            //    ComputerId = _metadataHandler.GetComputerIp(),
            //    Port = _metadataHandler.GetAvailablePort()
            //};

            //var response = await client.DownloadFileAsync(request);
            //if (response.Success == true)
            //{
            //    foreach (var file in response.Files)
            //    {
            //        var filePath = Path.Combine($"{_metadataHandler.GetSyncPath()}/{file.FileName}.{file.FileType}");
            //        if (!File.Exists(filePath))
            //        {
            //            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            //            await File.WriteAllBytesAsync(filePath, file.FileContent.ToByteArray());
            //        }
            //    }
            //}
        }
        
        public async Task<Common.GRPC.UploadResponse> UploadFileAsync(string fileName, byte[] fileContent)
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(fileName, availableServers);

            using var channel = GrpcChannel.ForAddress($"http://{responsibleServer.Address}:{responsibleServer.Port}");
            var client = new DistributedFileServerClient(channel);

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string fileExtension = Path.GetExtension(fileName).Replace(".", "");
            Timestamp creationDate = DateTimeConverter.ConvertToTimestamp(DateTime.Now);

            var clientIp = _metadataHandler.GetComputerIp();
            var clientPort = _metadataHandler.GetAvailablePort();

            var response = await client.UploadFileAsync(new Common.GRPC.UploadRequest
            {
                FileName = fileNameWithoutExtension,
                FileContent = Google.Protobuf.ByteString.CopyFrom(fileContent),
                FileType = fileExtension,
                CreationDate = creationDate,
                UserId = Session.UserId,
                ComputerId = clientIp,
                Port = clientPort
            });

            return response;
        }

        public async Task<Common.GRPC.DeleteResponse> DeleteFileAsync(string fileName)
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(fileName, availableServers);

            using var channel = GrpcChannel.ForAddress($"http://{responsibleServer.Address}:{responsibleServer.Port}");
            var client = new DistributedFileServerClient(channel);

            var response = await client.DeleteFileAsync(new Common.GRPC.DeleteRequest
            {
                FileName = fileName,
                UserId = Session.UserId,
                ComputerId = _metadataHandler.GetComputerIp(),
                Port = _metadataHandler.GetAvailablePort()
            });

            return response;
        }

        public async Task<Common.GRPC.UserDataResponse> RegisterUserAsync(string username, string password)
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(username, availableServers);

            var userResponse = await _accountService.CreateNewUserAsync(username, password, responsibleServer);

            if (userResponse.Success)
            {
                Session.UserId = userResponse.UserId;
                Session.Username = userResponse.Username;
            }

            return userResponse;
        }

        public async Task<Common.GRPC.UserDataResponse> LoginUserAsync(string username, string password)
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(username, availableServers);

            var userResponse = await _accountService.LoginUserAsync(username, password, responsibleServer);

            if (userResponse.Success)
            {
                Session.UserId = userResponse.UserId;
                Session.Username = userResponse.Username;
            }

            return userResponse;
        }

        public async Task<Common.GRPC.PingResponse> LogoutUser()
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(_metadataHandler.GetComputerIp(), availableServers);

            var response = await _accountService.SendPingToServers(responsibleServer, true);

            if (response.Success)
            {
                Session.ClearSession();
            }

            return response;
        }

        public async Task SendPingToServer()
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(_metadataHandler.GetComputerIp(), availableServers);

            await _accountService.SendPingToServers(responsibleServer, false);
        }

        public async Task SyncFileFromServerAsync(Common.GRPC.TransferRequest request, Common.GRPC.DeleteRequest deleteRequest)
        {
            try
            {
                if (request != null)
                {
                    var filePath = Path.Combine($"{_metadataHandler.GetSyncPath()}/{request.FileName}.{request.FileType}");
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    await File.WriteAllBytesAsync(filePath, request.FileContent.ToByteArray());
                    Console.WriteLine($"File {request.FileName}.{request.FileType} synced successfully.");
                }
                else if (deleteRequest != null)
                {
                    var deleteFilePath = Path.Combine($"{_metadataHandler.GetSyncPath()}/{deleteRequest.FileName}");
                    if (File.Exists(deleteFilePath))
                    {
                        File.Delete(deleteFilePath);
                        Console.WriteLine($"File {deleteRequest.FileName} deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"File {deleteRequest.FileName} not found for deletion.");
                    }
                }
                else
                {
                    Console.WriteLine("No request provided for syncing or deleting a file.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving synced file: {ex.Message}");
            }
        }
    }
}