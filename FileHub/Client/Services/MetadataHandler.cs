using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;

public static class MetadataHandler
{
    private static string GetMetadataFilePath()
    {
        string appDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
        string dataDirectory = Path.Combine(appDirectory, "Data");

        if (!Directory.Exists(dataDirectory))
        {
            Directory.CreateDirectory(dataDirectory);
        }

        return Path.Combine(dataDirectory, "metadata.json");
    }

    public static void SaveMetadata(string computerIp, string syncPath)
    {
        string metadataFilePath = GetMetadataFilePath();
        List<Metadata> metadataList = LoadAllMetadata();

        var existingMetadata = metadataList.Find(m => m.ComputerId == computerIp);

        if (existingMetadata != null)
        {
            existingMetadata.ComputerId = computerIp;
            existingMetadata.SyncPath = syncPath;
        }
        else
        {
            metadataList.Add(new Metadata
            {
                ComputerId = computerIp,
                SyncPath = syncPath
            });
        }

        File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(metadataList));
    }

    public static List<Metadata> LoadAllMetadata()
    {
        string metadataFilePath = GetMetadataFilePath();

        if (!File.Exists(metadataFilePath))
        {
            return new List<Metadata>();
        }

        var content = File.ReadAllText(metadataFilePath);

        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<Metadata>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Metadata>>(content) ?? new List<Metadata>();
        }
        catch (JsonException)
        {
            return new List<Metadata>();
        }
    }

    public static Metadata GetMetadataForComputer(string computerId)
    {
        var metadataList = LoadAllMetadata();
        return metadataList.Find(m => m.ComputerId == computerId);
    }

    public static string GetComputerIp()
    {
        var localIpAddress = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                          nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                          !nic.Description.ToLower().Contains("virtual") &&
                          !nic.Description.ToLower().Contains("pseudo"))
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(ip => ip.Address)
            .FirstOrDefault();

        if (localIpAddress == null)
        {
            throw new Exception("Nie można znaleźć odpowiedniego lokalnego adresu IP.");
        }

        return localIpAddress.ToString();
    }

    public static string GetDefaultSyncPath()
    {
        string syncPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SyncedFiles");
        Directory.CreateDirectory(syncPath);
        return syncPath;
    }

    public static int GetAvailablePort()
    {
        using (var tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 0))
        {
            tcpListener.Start();
            int port = ((System.Net.IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }
    }

    private static string GetComputerId()
    {
        var macAddress = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault();

        return macAddress ?? Guid.NewGuid().ToString();
    }
}
