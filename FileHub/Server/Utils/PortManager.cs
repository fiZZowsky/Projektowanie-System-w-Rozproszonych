using System.Net.Sockets;
using System.Net;

namespace Server.Utils;

public static class PortManager
{
    public static int FindAvailablePort(int startPort, int endPort)
    {
        for (int port = startPort; port <= endPort; port++)
        {
            if (IsPortAvailable(port))
            {
                return port;
            }
        }
        Console.WriteLine("No ports available within the specified range.");
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