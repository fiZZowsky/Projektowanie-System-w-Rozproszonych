using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Server.Services;

public class MulticastService
{
    public void AnnouncePresence(int port)
    {
        using var client = new UdpClient();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.MulticastAddress), AppConfig.MulticastPort);

        byte[] message = Encoding.UTF8.GetBytes($"Server:{port}");
        client.Send(message, message.Length, endPoint);

        Console.WriteLine($"[Multicast] Announced presence on port {port}.");
    }

    public async Task ListenForServersAsync(Action<int> onServerDiscovered)
    {
        using var client = new UdpClient();
        IPEndPoint localEp = new IPEndPoint(IPAddress.Any, AppConfig.MulticastPort);

        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.ExclusiveAddressUse = false;

        client.Client.Bind(localEp);
        client.JoinMulticastGroup(IPAddress.Parse(AppConfig.MulticastAddress));

        Console.WriteLine("[Multicast] Listening for multicast announcements...");

        while (true)
        {
            var result = await client.ReceiveAsync();
            string message = Encoding.UTF8.GetString(result.Buffer);

            if (message.StartsWith("Server:"))
            {
                int port = int.Parse(message.Substring("Server:".Length));
                onServerDiscovered?.Invoke(port);
            }
        }
    }
}