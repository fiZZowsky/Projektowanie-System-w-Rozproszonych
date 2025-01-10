using Client.Utils;
using Common.Converters;
using Common.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using System.IO;
using System.Net.NetworkInformation;
using static Common.GRPC.DistributedFileServer;

namespace Client.Services
{
    public class ClientService
    {
        private readonly string _serverAddress;

        public ClientService(string serverAddress)
        {
            _serverAddress = serverAddress;
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

        public async Task<ActiveUserModel> GetClientAddressAndPortAsync()
        {
            try
            {
                // Pobierz adres IP klienta
                var localIpAddress = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                if (localIpAddress == null)
                {
                    throw new Exception("Nie można określić lokalnego adresu IP klienta.");
                }

                int clientPort = _clientPort;

                var user = new ActiveUserModel
                {
                    ClientAddress = localIpAddress.ToString(),
                    ClientPort = clientPort
                };

                return (user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas pobierania adresu i portu klienta: {ex.Message}");
                throw;
            }
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

            var clientInfo = await GetClientAddressAndPortAsync();

            var response = await client.UploadFileAsync(new Common.GRPC.UploadRequest
            {
                FileName = fileNameWithoutExtension,
                FileContent = Google.Protobuf.ByteString.CopyFrom(fileContent),
                FileType = fileExtension,
                CreationDate = creationDate,
                UserId = Session.UserId,

            });

            return response;
        }

        public async Task<Common.GRPC.DownloadResponse> DownloadFileAsync(string userId)
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            var client = new DistributedFileServerClient(channel);

            var response = await client.DownloadFileAsync(new Common.GRPC.DownloadRequest
            {
                UserId = userId
            });

            return response;
        }

        public async Task<Common.GRPC.DeleteResponse> DeleteFileAsync(string fileName)
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            var client = new DistributedFileServerClient(channel);

            var response = await client.DeleteFileAsync(new Common.GRPC.DeleteRequest
            {
                FileName = fileName
            });

            return response;
        }

        public async Task<Common.GRPC.UserDataResponse> RegisterUserAsync(string username, string password)
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(username, availableServers);

            AccountService _accountService = new AccountService();
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

            AccountService _accountService = new AccountService();
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
            var responsibleServer = FindResponsibleServer(GetComputerId(), availableServers);

            AccountService _accountService = new AccountService();
            var response = await _accountService.SendPingToServers(responsibleServer, true);

            if (response.Success)
            {
                Session.ClearSession();
            }

            return response;
        }

        public static string GetComputerId()
        {
            var macAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            return macAddress ?? Guid.NewGuid().ToString();
        }

        public async Task SendPingToServer()
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(GetComputerId(), availableServers);

            AccountService _accountService = new AccountService();
            await _accountService.SendPingToServers(responsibleServer, false);
        }

        public async Task SyncFileFromServerAsync(Common.GRPC.TransferRequest request)
        {
            try
            {
                var filePath = Path.Combine("SyncedFiles", $"{request.FileName}.{request.FileType}");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                await File.WriteAllBytesAsync(filePath, request.FileContent.ToByteArray());
                Console.WriteLine($"File {request.FileName}.{request.FileType} synced successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving synced file: {ex.Message}");
            }
        }

        public class ClientGrpcServer : Common.GRPC.DistributedFileServer.DistributedFileServerBase
        {
            private readonly ClientService _clientService;

            public ClientGrpcServer(ClientService clientService)
            {
                _clientService = clientService;
            }

            public override async Task<Common.GRPC.TransferResponse> TransferFile(Common.GRPC.TransferRequest request, ServerCallContext context)
            {
                try
                {
                    await _clientService.SyncFileFromServerAsync(request);
                    return new Common.GRPC.TransferResponse { Success = true };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during file transfer: {ex.Message}");
                    return new Common.GRPC.TransferResponse { Success = false };
                }
            }
        }
    }
}