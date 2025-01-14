using Common.Converters;
using Common.GRPC;
using Common.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Server.Models;
using Server.Utils;
using System.Text.Json;

namespace Server.Services
{
    public class ServerService : DistributedFileServer.DistributedFileServerBase
    {
        private readonly string _path;
        private readonly DHTService _dhtService;
        private FilesService _filesService;
        private readonly UserService _userService;

        public ServerService(string filesDirectoryPath, DHTService dhtService)
        {
            _path = filesDirectoryPath;
            _dhtService = dhtService;
            _filesService = new FilesService(filesDirectoryPath, _dhtService);
            _userService = new UserService(AppConfig.DefaultUserDataStoragePath, _dhtService.GetServerPort());

            StartInactivityCheck();
        }

        public override async Task<NodeListResponse> GetNodes(Empty request, ServerCallContext context)
        {
            var nodes = _dhtService.GetNodes();
            return ModelConverter.FromDHTNodes(nodes);
        }

        public override async Task<UploadResponse> UploadFile(UploadRequest request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"[Upload] [{request.CreationDate}] File received: {request.FileName}.{request.FileType}");

                var availableNodes = _dhtService.GetNodes();
                var node = DHTManager.FindResponsibleNode(request.FileName, availableNodes);

                if (node.Port != _dhtService.GetServerPort()) // Jeśli inny serwer odpowiada za plik
                {
                    Console.WriteLine($"[Redirect] Forwarding upload to server at {node.Address}:{node.Port}");
                    bool success = await _filesService.SendFileToServer(request, node.Address, node.Port);

                    return new UploadResponse
                    {
                        Success = success,
                        Message = success
                            ? "File forwarded and uploaded successfully to the responsible server."
                            : "Failed to forward and upload the file to the responsible server."
                    };
                }

                var response = await _filesService.SaveFile(request);
                Console.WriteLine($"[Upload] {request.FileName}.{request.FileType} Status succeeded: {response.Success}");

                var usersToSync = _userService.GetUsersToSync(request.UserId, request.ComputerId, request.Port);
                if (usersToSync.Count > 0)
                {
                    foreach (var user in usersToSync)
                    {
                        var fileData = new TransferRequest
                        {
                            UserId = user.UserId.ToString(),
                            FileName = request.FileName,
                            FileContent = request.FileContent,
                            FileType = request.FileType,
                            CreationDate = request.CreationDate
                        };

                        bool syncSuccess = await SyncFileToClient(fileData, user.ComputerId, user.ClientPort);
                        if (!syncSuccess)
                        {
                            Console.WriteLine($"[Sync] Failed to send file to User ID: {user.UserId}, Computer ID: {user.ComputerId}");
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas przesyłania pliku: {ex.Message}");
                return new UploadResponse
                {
                    Success = false,
                    Message = $"Error uploading file: {ex.Message}"
                };
            }
        }

        public override async Task<DownloadResponse> DownloadFile(DownloadRequest request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"[Download] Requested by: {request.UserId}");
                var response = await _filesService.GetUserFiles(request);
                Console.WriteLine($"[Download] {request.UserId} Status seccessed: {response.Success}");
                return response;
            }
            catch (Exception ex)
            {
                return new DownloadResponse
                {
                    Success = false,
                    Message = $"Error downloading files: {ex.Message}"
                };
            }
        }

        public override async Task<DownloadByServerResponse> DownloadFileByServer(DownloadByServerRequest request, ServerCallContext context)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName + "\\Server\\DataStorage\\", request.FileName);
                if (File.Exists(filePath))
                {
                    var fileContent = await File.ReadAllBytesAsync(filePath);
                    var fileData = new FileData
                    {
                        FileName = request.FileName,
                        FileContent = ByteString.CopyFrom(fileContent),
                        FileType = "application/octet-stream", // Zmień w zależności od typu pliku
                        CreationDate = Timestamp.FromDateTime(File.GetCreationTime(filePath).ToUniversalTime())
                    };

                    return new DownloadByServerResponse
                    {
                        Success = true,
                        Message = "File successfully retrieved.",
                        Files = { fileData }
                    };
                }
                else
                {
                    return new DownloadByServerResponse
                    {
                        Success = false,
                        Message = "File not found.",
                        Files = { }
                    };
                }
            }
            catch (Exception ex)
            {
                return new DownloadByServerResponse
                {
                    Success = false,
                    Message = $"Error downloading file: {ex.Message}",
                    Files = { }
                };
            }
        }

        public override async Task<DeleteResponse> DeleteFile(DeleteRequest request, ServerCallContext context)
        {
            string filePath = Path.Combine(_path, request.FileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"[Serwer] Plik {request.FileName} został usunięty.");
                return new DeleteResponse { Success = true, Message = "Plik usunięty." };
            }
            else
            {
                return new DeleteResponse { Success = false, Message = "Plik nie znaleziony." };
            }
        }

        public override async Task<TransferResponse> TransferFile(TransferRequest request, ServerCallContext context)
        {
            try
            {
                var filePath = Path.Combine("_path", request.FileName);
                await File.WriteAllBytesAsync(filePath, request.FileContent.ToByteArray());
                return new TransferResponse { Success = true, Message = "File transferred successfully." };
            }
            catch (Exception ex)
            {
                return new TransferResponse { Success = false, Message = $"Error transferring file: {ex.Message}" };
            }
        }

        public override async Task<UserDataResponse> RegisterNewUser(UserDataRequest request, ServerCallContext context)
        {
            try
            {
                var users = await _userService.GetUsers();
                if (users.Any(u => u.Username == request.Username))
                {
                    return new UserDataResponse { Success = false, Message = "Username already exists." };
                }
                var newUser = new UserModel { Id = users.Count() + 1, Username = request.Username, Password = request.PasswordHash };
                users.Add(newUser);
                var IsSuccessed = await _userService.AddNewUser(users);

                if(IsSuccessed)
                {
                    return new UserDataResponse { Success = true, Message = "User registered successfully", UserId = newUser.Id.ToString(), Username = newUser.Username };
                }
                else
                {
                    return new UserDataResponse { Success = false, Message = "Error registering new user" };
                }
            }
            catch (Exception ex)
            {
                return new UserDataResponse { Success = false, Message = $"Error registering new user: {ex.Message}" };
            }
        }

        public override async Task<UserDataResponse> LoginUser(UserDataRequest request, ServerCallContext context)
        {
            try
            {
                var users = await _userService.GetUsers();
                var user = users.FirstOrDefault(u => u.Username == request.Username && u.Password == request.PasswordHash);

                if (user != null)
                {
                    // Odpowiedź serwera
                    return new UserDataResponse
                    {
                        Success = true,
                        Message = "User logged in successfully",
                        UserId = user.Id.ToString(),
                        Username = user.Username
                    };
                }

                return new UserDataResponse { Success = false, Message = "Incorrect user data" };
            }
            catch (Exception ex)
            {
                return new UserDataResponse { Success = false, Message = $"User login error: {ex.Message}" };
            }
        }

        public override async Task<PingResponse> Ping(PingRequest request, ServerCallContext context)
        {
            Console.WriteLine($"[Server] Received ping from user id {request.UserId}");
            if (await _userService.PingToUser(request) == true)
            {
                return new PingResponse { Success = true, Message = "Ping received" };
            }
            else
            {
                return new PingResponse { Success = false, Message = $"Encountered an error during ping process to user: {request.UserId}" };
            }
        }

        public void StartInactivityCheck()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    _userService.RemoveInactiveUsers();
                    await Task.Delay(TimeSpan.FromMinutes(AppConfig.InactivityCheckTime));
                }
            });
        }

        public void UpdateClientsList(string serializedClientsList)
        {
            var updatedClientsList = JsonSerializer.Deserialize<List<ActiveUserModel>>(serializedClientsList);

            _userService.UpdateActiveUsersList(updatedClientsList);
        }

        public async Task<bool> SyncFileToClient(TransferRequest request, string clientAddress, int clientPort)
        {
            try
            {
                using var channel = GrpcChannel.ForAddress($"http://{clientAddress}:{clientPort}");
                var client = new DistributedFileServer.DistributedFileServerClient(channel);

                var response = await client.TransferFileAsync(request);
                return response.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendFileToClient] Error sending file to {clientAddress}:{clientPort} - {ex.Message}");
                return false;
            }
        }
    }
}