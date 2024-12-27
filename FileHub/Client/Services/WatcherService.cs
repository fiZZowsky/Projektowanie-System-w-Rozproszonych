using System;
using System.IO;

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
            try
            {
                Console.WriteLine($"[FolderWatcher] Plik utworzony: {e.FullPath}");

                if (File.Exists(e.FullPath))
                {
                    var fileContent = File.ReadAllBytes(e.FullPath);
                    await _clientService.UploadFileAsync(e.FullPath, fileContent);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas przesyłania pliku {e.FullPath}: {ex.Message}");
            }
        }

        private async void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                Console.WriteLine($"[FolderWatcher] Plik usunięty: {e.FullPath}");
                await _clientService.NotifyFileDeletedAsync(e.FullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas usuwania pliku {e.FullPath}: {ex.Message}");
            }
        }

        private async void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"[FolderWatcher] Plik zmieniony z {e.OldFullPath} na {e.FullPath}");
            await _clientService.NotifyFileDeletedAsync(e.OldFullPath);

            if (File.Exists(e.FullPath))
            {
                var fileContent = File.ReadAllBytes(e.FullPath);
                await _clientService.UploadFileAsync(e.FullPath, fileContent);
            }
        }
    }
}
