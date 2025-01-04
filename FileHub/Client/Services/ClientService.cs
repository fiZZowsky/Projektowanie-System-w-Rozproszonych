using Common.Converters;
using Common.GRPC;
using Google.Protobuf.WellKnownTypes;
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

        public async Task UploadFileAsync(string fileName, byte[] fileContent)
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
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
                UserId = "user123"
            });

            if (response.Success)
            {
                Console.WriteLine($"Plik {fileName} przesłany.");
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

            //if (response.Success)
            //{
            //    File.WriteAllBytes(fileName, response.FileContent.ToByteArray());
            //    Console.WriteLine($"Plik {fileName} pobrany.");
            //}
            //else
            //{
            //    Console.WriteLine($"Błąd podczas pobierania plikow dla uzytkownika {fileName}.");
            //}
        }

        public async Task DeleteFileAsync(string fileName)
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

        public static string GetComputerId()
        {
            var macAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            return macAddress ?? Guid.NewGuid().ToString();
        }
    }

}
