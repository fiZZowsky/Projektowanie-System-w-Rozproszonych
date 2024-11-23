using Common.GRPC;
using Grpc.Net.Client;
using System.IO;
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

            var response = await client.UploadFileAsync(new UploadRequest
            {
                FileName = fileName,
                FileContent = Google.Protobuf.ByteString.CopyFrom(fileContent)
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

        public async Task DownloadFileAsync(string fileName)
        {
            using var channel = GrpcChannel.ForAddress(_serverAddress);
            var client = new DistributedFileServerClient(channel);

            var response = await client.DownloadFileAsync(new DownloadRequest
            {
                FileName = fileName
            });

            if (response.Success)
            {
                File.WriteAllBytes(fileName, response.FileContent.ToByteArray());
                Console.WriteLine($"Plik {fileName} pobrany.");
            }
            else
            {
                Console.WriteLine($"Błąd podczas pobierania pliku {fileName}.");
            }
        }
    }
}
