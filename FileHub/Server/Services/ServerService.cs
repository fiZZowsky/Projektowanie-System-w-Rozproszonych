using Common;
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

                        bool syncSuccess = await SyncFileToClient(fileData, null, user.ComputerId, user.ClientPort);
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

        public override async Task<DownloadResponse> DownloadLocalFiles(DownloadRequest request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"[Download] Requested by server.");
                var response = await _filesService.GetLocalUserFiles(request);
                Console.WriteLine($"[Download] Status seccessed: {response.Success}");
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
                var filePath = Path.Combine(_path, request.FileName);

                if (File.Exists(filePath))
                {
                    // Sprawdzamy, czy plik jest w użyciu
                    bool fileLocked = true;
                    DateTime startTime = DateTime.Now;

                    while (fileLocked && (DateTime.Now - startTime).TotalMinutes < 1)
                    {
                        try
                        {
                            // Próbujemy otworzyć plik do odczytu z blokadą
                            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
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
                        return new DownloadByServerResponse
                        {
                            Success = false,
                            Message = "File is currently in use and could not be accessed within the time limit.",
                            Files = { }
                        };
                    }

                    // Jeśli plik jest dostępny, odczytujemy jego zawartość
                    var fileContent = await File.ReadAllBytesAsync(filePath);
                    var fileInfo = new FileInfo(filePath);
                    var decodedFile = await FormatConverter.DecodeFileDataFromName(fileInfo);
                    var fileData = new FileData
                    {
                        FileName = request.FileName,
                        FileContent = ByteString.CopyFrom(fileContent),
                        FileType = decodedFile.FileType,
                        CreationDate = decodedFile.CreationDate
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
            var response = await _filesService.GetUserFiles(request);

            if (response != null && response.Files.Count() > 0)
            {
                var file = response.Files.FirstOrDefault(x => x.FileName + "." + x.FileType == request.FileName);
                if (file != null)
                {
                    if (_dhtService.GetServerPort() != file.ServerId)
                    {
                        var fileName = $"{file.FileName}~{file.FileType}~{request.UserId}";

                        var deleteRequest = new DeleteRequest
                        {
                            UserId = request.UserId,
                            ComputerId = request.ComputerId,
                            FileName = fileName
                        };

                        using var channel = GrpcChannel.ForAddress($"http://{AppSettings.DefaultAddress}:{file.ServerId}");
                        var client = new DistributedFileServer.DistributedFileServerClient(channel);
                        var deleteResponse = client.DeleteFileFromServer(deleteRequest);

                        return new DeleteResponse { Success = deleteResponse.Success, Message = deleteResponse.Message };
                    }
                    else
                    {
                        string sanitizedFileName = request.FileName.Replace(".", "~");
                        var matchingFiles = Directory.GetFiles(_path, $"*{sanitizedFileName}*");

                        if (matchingFiles.Length > 0)
                        {
                            var fileToDelete = matchingFiles.First();
                            File.Delete(fileToDelete);
                            Console.WriteLine($"[Serwer] Plik {request.FileName} został usunięty.");

                            var usersToSync = _userService.GetUsersToSync(request.UserId, request.ComputerId, request.Port);
                            if (usersToSync.Count > 0)
                            {
                                foreach (var user in usersToSync)
                                {
                                    var fileData = new DeleteRequest
                                    {
                                        UserId = user.UserId.ToString(),
                                        FileName = request.FileName
                                    };

                                    bool syncSuccess = await SyncFileToClient(null, fileData, user.ComputerId, user.ClientPort);
                                    if (!syncSuccess)
                                    {
                                        Console.WriteLine($"[Sync] Failed to send file deletion command to User ID: {user.UserId}, Computer ID: {user.ComputerId}");
                                    }
                                }
                            }

                            return new DeleteResponse { Success = true, Message = "Plik usunięty." };
                        }
                        else
                        {
                            return new DeleteResponse { Success = false, Message = "Plik nie znaleziony." };
                        }
                    }
                }
            }

            return new DeleteResponse { Success = false, Message = "Brak plików do usunięcia lub nie znaleziono wskazanego pliku." };
        }

        public override async Task<DeleteResponse> DeleteFileFromServer(DeleteRequest request, ServerCallContext context)
        {
            var matchingFiles = Directory.GetFiles(_path, $"*{request.FileName}*");
            if (matchingFiles.Length > 0)
            {
                var fileToDelete = matchingFiles.First();
                File.Delete(fileToDelete);

                Console.WriteLine($"[Server] Usunięto plik {Path.GetFileName(fileToDelete)}");
                return new DeleteResponse { Success = true, Message = $"Usunięto plik {Path.GetFileName(fileToDelete)}" };
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
                var transferRequest = new UploadRequest
                {
                    CreationDate = request.CreationDate,
                    FileContent = request.FileContent,
                    FileName = request.FileName,
                    FileType = request.FileType,
                    UserId = request.UserId
                };

                var response = await _filesService.SaveFile(transferRequest);
                return new TransferResponse { Success = response.Success, Message = "File transferred successfully." };
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

        public async Task<bool> SyncFileToClient(TransferRequest request, DeleteRequest delRequest, string clientAddress, int clientPort)
        {
            try
            {
                using var channel = GrpcChannel.ForAddress($"http://{clientAddress}:{clientPort}");
                var client = new DistributedFileServer.DistributedFileServerClient(channel);

                if (request != null)
                {
                    var response = await client.TransferFileAsync(request);
                    return response.Success;
                }
                else if (delRequest != null)
                {
                    var response = await client.DeleteFileAsync(delRequest);
                    return response.Success;
                }
                else
                {
                    Console.WriteLine("[SyncFileToClient] Both TransferRequest and DeleteRequest are null.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncFileToClient] Error sending file to {clientAddress}:{clientPort} - {ex.Message}");
                return false;
            }
        }

    }
}