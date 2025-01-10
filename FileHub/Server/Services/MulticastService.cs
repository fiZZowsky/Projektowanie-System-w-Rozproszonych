using Server;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;
using Grpc.Core;

public class MulticastService
{
    private readonly int _port;

    public MulticastService(int port)
    {
        _port = port;
    }

    public void AnnouncePresence(int port)
    {
        Task.Delay(100).Wait();
        using var client = new UdpClient();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.MulticastAddress), AppConfig.MulticastPort);

        byte[] message = Encoding.UTF8.GetBytes($"[{_port}]Server:{port}");
        client.Send(message, message.Length, endPoint);

        Console.WriteLine($"[Multicast] Announced presence on port {port}.");
    }

    public void AnnounceShutdown(int port)
    {
        using var client = new UdpClient();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.MulticastAddress), AppConfig.MulticastPort);

        byte[] message = Encoding.UTF8.GetBytes($"[{_port}]Shutdown:{port}");
        client.Send(message, message.Length, endPoint);

        Console.WriteLine($"[Multicast] Announced shutdown on port {port}.");
    }

    public async Task AnnounceNodesList(int port, string serializedServerList)
    {
        Task.Delay(100).Wait();
        using var client = new UdpClient();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.MulticastAddress), AppConfig.MulticastPort);

        byte[] message = Encoding.UTF8.GetBytes($"To:{port}:{serializedServerList}");
        await client.SendAsync(message, message.Length, endPoint);

        Console.WriteLine($"[Multicast] Announced updated nodes list.");
    }

    public async Task ListenForServersAsync(
        Action<int> onServerDiscovered,
        Action<int> onServerShutdown,
        Action<string> onServerGetServersList,
        Action<string> onServerGetClientsList)
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

            if (message.StartsWith($"[{_port}]Server:"))
            {
                continue;
            }
            else if (!message.StartsWith($"[{_port}]Server:") && message.Contains("Server:"))
            {
                int port = int.Parse(message.Substring($"[{_port}]Server:".Length));
                onServerDiscovered?.Invoke(port);
            }
            else if (message.StartsWith($"[{_port}]Shutdown:"))
            {
                continue;
            }
            else if (!message.StartsWith($"[{_port}]Shutdown:") && message.Contains("Shutdown:"))
            {
                int port = int.Parse(message.Substring($"[{_port}]Shutdown:".Length));
                onServerShutdown?.Invoke(port);
            }
            else if (message.StartsWith($"To:{_port}:"))
            {
                string serializedServerList = message.Substring($"To:{_port}:".Length);
                onServerGetServersList?.Invoke(serializedServerList);
            }
            else if (message.Contains($"[{_port}]Clients:"))
            {
                continue;
            }
            else if (!message.StartsWith($"[{_port}]Clients:") && message.Contains("Clients:"))
            {
                string serializedClientList = message.Substring($"[{_port}]Clients:".Length);
                onServerGetClientsList?.Invoke(serializedClientList);
            }
        }
    }
}