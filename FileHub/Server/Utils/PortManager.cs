using System.Net.Sockets;
using System.Net;

namespace Server.Utils;

public static class PortManager
{
    public static int FindAvailablePort(int startPort, int endPort)
    {
        Random random = new Random();
        for (int i = 0; i < 10; i++)
        {
            int port = random.Next(startPort, endPort + 1);
            if (IsPortAvailable(port))
            {
                return port;
            }
        }
        return -1;
    }

    private static bool IsPortAvailable(int port)
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
            return false;
        }
    }
}