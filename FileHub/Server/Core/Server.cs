using System.Net;
using System.Net.Sockets;
using Server.FileManagement;
using Server.Networking;

namespace Server.Core
{
    public class Server
    {
        private readonly string serverId;
        private readonly int port;
        private readonly FileManager fileManager;
        private TcpListener tcpListener;

        public Server(string serverId, int port)
        {
            this.serverId = serverId;
            this.port = port;
            fileManager = new FileManager(serverId);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();

            Console.WriteLine($"Serwer {serverId} uruchomiony na porcie {port}...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    _ = Task.Run(() => new ClientHandler(tcpClient, fileManager, cancellationToken).HandleClientAsync());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd przy akceptowaniu klienta: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            tcpListener.Stop();
            Console.WriteLine($"Serwer {serverId} zatrzymany.");
        }
    }
}