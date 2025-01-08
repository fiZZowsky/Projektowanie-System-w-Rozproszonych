using System.IO;
using System.Windows;

namespace Client.Services
{
    public class WatcherService
    {
        private readonly FileSystemWatcher _watcher;
        private readonly ClientService _clientService;

        public WatcherService(string path, ClientService clientService)
        {
            _clientService = clientService;

            _watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                Filter = "*.*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;
        }

        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            const int maxFileSize = 4 * 1024 * 1024; // 4 MB

            if (File.Exists(e.FullPath))
            {
                var fileInfo = new FileInfo(e.FullPath);
                double fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                double maxFileSizeMB = maxFileSize / (1024.0 * 1024.0);

                if (fileInfo.Length > maxFileSize)
                {
                    Console.WriteLine($"[FolderWatcher] Plik jest za duży: {fileSizeMB:F2} MB. Maksymalny rozmiar to {maxFileSizeMB:F2} MB.");
                    MessageBox.Show($"Plik {e.FullPath} jest za duży ({fileSizeMB:F2} MB). Maksymalny rozmiar to {maxFileSizeMB:F2} MB.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var fileContent = File.ReadAllBytes(e.FullPath);
                var response = await _clientService.UploadFileAsync(e.FullPath, fileContent);

                if (!response.Success)
                {
                    MessageBox.Show(response.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            var response = await _clientService.NotifyFileDeletedAsync(e.FullPath);
            if (!response.Success)
            {
                MessageBox.Show(response.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"[FolderWatcher] Plik zmieniony z {e.OldFullPath} na {e.FullPath}");

            var deleteResponse = await _clientService.NotifyFileDeletedAsync(e.OldFullPath);
            if(deleteResponse.Success)
            {
                if (File.Exists(e.FullPath))
                {
                    var fileContent = File.ReadAllBytes(e.FullPath);
                    var uploadResponse = await _clientService.UploadFileAsync(e.FullPath, fileContent);

                    if (!uploadResponse.Success)
                    {
                        MessageBox.Show(uploadResponse.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show(deleteResponse.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
