using Client.Utils;
using Common.Converters;
using Common.GRPC;
using Common.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;
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

        public async Task<List<NodeInfo>> GetAvailableServersAsync()
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            var client = new DistributedFileServerClient(channel);

            var response = await client.GetNodesAsync(new Empty());
            return response.Nodes.ToList();
        }

        private NodeInfo FindResponsibleServer(string fileName, List<NodeInfo> availableServers)
        {
            int hash = Math.Abs(fileName.GetHashCode());
            int serverIndex = hash % availableServers.Count; // Równomierne rozproszenie po serwerach
            return availableServers[serverIndex];
        }

        public async Task UploadFileAsync(string fileName, byte[] fileContent)
        {
            var availableServers = await GetAvailableServersAsync();
            var responsibleServer = FindResponsibleServer(fileName, availableServers);

            using var channel = GrpcChannel.ForAddress($"http://{responsibleServer.Address}:{responsibleServer.Port}");
            var client = new DistributedFileServerClient(channel);

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string fileExtension = Path.GetExtension(fileName).Replace(".", "");
            Timestamp creationDate = DateTimeConverter.ConvertToTimestamp(DateTime.Now);

            var response = await client.UploadFileAsync(new UploadRequest
            {
                FileName = fileNameWithoutExtension,
                FileContent = Google.Protobuf.ByteString.CopyFrom(fileContent),
                FileType = fileExtension,
                CreationDate = creationDate,
                UserId = Session.UserId
            });

            if (response.Success)
            {
                Console.WriteLine($"Plik {fileName} przesłany na serwer {responsibleServer.Address}:{responsibleServer.Port}");
            }
            else
            {
                Console.WriteLine($"Błąd podczas przesyłania pliku {fileName}.");
            }
        }

        public async Task DownloadFileAsync(string userId)
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            var client = new DistributedFileServerClient(channel);

            var response = await client.DownloadFileAsync(new DownloadRequest
            {
                UserId = userId
            });

            if (response.Success)
            {
                string fileName = $"{response.FileName}.{response.FileType}";
                File.WriteAllBytes(fileName, response.FileContent.ToByteArray());
                Console.WriteLine($"Plik {fileName} pobrany.");
            }
            else
            {
                Console.WriteLine($"Błąd podczas pobierania plików dla użytkownika {userId}: {response.Message}");
            }
        }

        public async Task NotifyFileDeletedAsync(string fileName)
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            var client = new DistributedFileServerClient(channel);

            var response = await client.DeleteFileAsync(new DeleteRequest
            {
                FileName = fileName
            });

            if (response.Success)
            {
                Console.WriteLine($"[Client] Plik {fileName} został usunięty na serwerze.");
            }
            else
            {
                Console.WriteLine($"[Client] Błąd podczas usuwania pliku {fileName}.");
            }
        }

        public async Task RegisterUserAsync(string username, string password)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = new byte[16];

                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(password);
                    cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    var encryptedPassword = Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }
    }
}