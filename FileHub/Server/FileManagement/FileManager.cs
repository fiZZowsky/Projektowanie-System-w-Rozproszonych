using System.Net.Sockets;

namespace Server.FileManagement
{
    public class FileManager
    {
        private readonly string serverDirectory;
        private readonly string serverInfoFilePath;
        private readonly string resourcesFolderPath;
        private readonly string metadataFilePath;
        private readonly Dictionary<string, string> fileMetadata;

        public FileManager(string serverId)
        {
            serverDirectory = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName, $"Server_{serverId}");

            if (!Directory.Exists(serverDirectory))
            {
                Directory.CreateDirectory(serverDirectory);
            }

            serverInfoFilePath = Path.Combine(serverDirectory, "server_info.txt");
            resourcesFolderPath = Path.Combine(serverDirectory, "resources");
            metadataFilePath = Path.Combine(serverDirectory, "metadata.txt");

            if (!Directory.Exists(resourcesFolderPath))
            {
                Directory.CreateDirectory(resourcesFolderPath);
            }

            if (!File.Exists(serverInfoFilePath))
            {
                File.WriteAllText(serverInfoFilePath, $"Serwer {serverId} - Data: {DateTime.Now}");
            }

            if (!File.Exists(metadataFilePath))
            {
                File.WriteAllText(metadataFilePath, "Plik metadata - Data: " + DateTime.Now);
            }

            fileMetadata = new Dictionary<string, string>();
        }

        public string UploadFile(string fileName, NetworkStream networkStream)
        {
            string uniqueFileName = GetUniqueFileName(fileName);
            string filePath = Path.Combine(resourcesFolderPath, uniqueFileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }

                fileMetadata[uniqueFileName] = filePath;
                return $"Plik {fileName} zapisany na serwerze jako {uniqueFileName}.";
            }
            catch (Exception ex)
            {
                return $"Błąd podczas przesyłania pliku {fileName}: {ex.Message}";
            }
        }

        public string DownloadFile(string fileName)
        {
            if (fileMetadata.ContainsKey(fileName))
            {
                string filePath = fileMetadata[fileName];
                return $"Plik {fileName} znajduje się w {filePath}.";
            }
            return "Plik nie znaleziony.";
        }

        private string GetUniqueFileName(string fileName)
        {
            string fileExtension = Path.GetExtension(fileName);
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string uniqueFileName = fileName;
            int counter = 1;

            while (fileMetadata.ContainsKey(uniqueFileName))
            {
                uniqueFileName = $"{baseName}_{counter}{fileExtension}";
                counter++;
            }

            return uniqueFileName;
        }
    }
}
