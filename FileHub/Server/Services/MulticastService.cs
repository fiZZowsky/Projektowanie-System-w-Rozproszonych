using Server;
using System.Net.Sockets;
using System.Net;
using System.Text;

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

    public void AnnounceShutdown(int port)
    {
        using var client = new UdpClient();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.MulticastAddress), AppConfig.MulticastPort);

        byte[] message = Encoding.UTF8.GetBytes($"Shutdown:{port}");
        client.Send(message, message.Length, endPoint);

        Console.WriteLine($"[Multicast] Announced shutdown on port {port}.");
    }

    //public void AnnounceFileChange(string filePath, string changeType)
    //{
    //    using var client = new UdpClient();
    //    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.MulticastAddress), AppConfig.MulticastPort);

    //    string message = $"FileChange:{changeType}:{filePath}";
    //    byte[] data = Encoding.UTF8.GetBytes(message);
    //    client.Send(data, data.Length, endPoint);

    //    Console.WriteLine($"[Multicast] Announced file change: {changeType} {filePath}");
    //}

    public async Task ListenForServersAsync(Action<int> onServerDiscovered, Action<int> onServerShutdown)
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
            else if (message.StartsWith("Shutdown:"))
            {
                int port = int.Parse(message.Substring("Shutdown:".Length));
                onServerShutdown?.Invoke(port);
            }
            //else if (message.StartsWith("FileChange:"))
            //{
            //    string[] parts = message.Split(':');
            //    string changeType = parts[1];
            //    string filePath = parts[2];

            //    Console.WriteLine($"[FileChange] {changeType}: {filePath}");

            //    if (changeType == "DELETE" && File.Exists(filePath))
            //    {
            //        File.Delete(filePath);
            //        Console.WriteLine($"[FileChange] Deleted {filePath}");
            //    }
            //}
        }
    }
}
