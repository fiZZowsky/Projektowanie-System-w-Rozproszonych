using System.IO;
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

    public static void SaveMetadata(Metadata metadata)
    {
        string metadataFilePath = GetMetadataFilePath();
        List<Metadata> metadataList = LoadAllMetadata();

        var existingMetadata = metadataList.Find(m => m.ComputerId == metadata.ComputerId);

        if (existingMetadata != null)
        {
            existingMetadata.SyncPath = metadata.SyncPath;
        }
        else
        {
            metadataList.Add(metadata);
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
}