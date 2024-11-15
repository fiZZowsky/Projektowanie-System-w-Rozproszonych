using System.Net.Sockets;
using Server.FileManagement;

namespace Server.Networking
{
    public class ClientHandler
    {
        private readonly TcpClient tcpClient;
        private readonly FileManager fileManager;
        private readonly CancellationToken cancellationToken;

        public ClientHandler(TcpClient tcpClient, FileManager fileManager, CancellationToken cancellationToken)
        {
            this.tcpClient = tcpClient;
            this.fileManager = fileManager;
            this.cancellationToken = cancellationToken;
        }

        public async Task HandleClientAsync()
        {
            using (var networkStream = tcpClient.GetStream())
            using (var reader = new StreamReader(networkStream))
            using (var writer = new StreamWriter(networkStream))
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var request = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(request)) break;

                        var response = ProcessRequest(request, networkStream);
                        await writer.WriteLineAsync(response);
                        await writer.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas komunikacji z klientem: {ex.Message}");
                }
            }

            tcpClient.Close();
        }

        private string ProcessRequest(string request, NetworkStream networkStream)
        {
            var parts = request.Split('|');
            var command = parts[0];

            switch (command)
            {
                case "UPLOAD":
                    return fileManager.UploadFile(parts[1], networkStream);
                case "DOWNLOAD":
                    return fileManager.DownloadFile(parts[1]);
                default:
                    return "Nieznana operacja.";
            }
        }
    }
}
