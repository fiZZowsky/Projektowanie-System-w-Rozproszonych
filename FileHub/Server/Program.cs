using Grpc.Core;
using Server.Services;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int startPort = 5000;
            int endPort = 6000;
            int selectedPort = FindAvailablePort(startPort, endPort);

            if (selectedPort == -1)
            {
                Console.WriteLine($"No available ports in range {startPort}-{endPort}. Exiting.");
                return;
            }

            var server = new Grpc.Core.Server
            {
                Services = { Common.GRPC.DistributedFileServer.BindService(new ServerService()) },
                Ports = { new ServerPort("localhost", selectedPort, ServerCredentials.Insecure) }
            };

            Console.WriteLine($"[Server] Starting on port {selectedPort}...");
            server.Start();

            Console.WriteLine("[Server] Press ENTER to stop the server.");
            Console.ReadLine();

            await server.ShutdownAsync();
            Console.WriteLine("[Server] Shutdown completed.");
        }

        static int FindAvailablePort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            return -1; // Brak dostępnych portów
        }

        static bool IsPortAvailable(int port)
        {
            try
            {
                using (var listener = new TcpListener(IPAddress.Loopback, port))
                {
                    listener.Start();
                    return true;
                }
            }
            catch (SocketException)
            {
                return false; // Port jest w użyciu
            }
        }
    }
}
