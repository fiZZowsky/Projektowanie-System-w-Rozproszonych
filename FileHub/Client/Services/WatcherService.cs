using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Collections.Concurrent;

namespace Client.Services
{
    public class WatcherService
    {
        public readonly FileSystemWatcher _watcher;
        private readonly ClientService _clientService;

        private static readonly ConcurrentDictionary<string, DateTime> _fileEventTimestamps = new();

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
            _watcher.Changed += OnFileChanged;
        }

        private bool ShouldProcessEvent(string filePath, int debounceMilliseconds = 1000)
        {
            var now = DateTime.Now;
            if (_fileEventTimestamps.TryGetValue(filePath, out var lastEventTime))
            {
                if ((now - lastEventTime).TotalMilliseconds < debounceMilliseconds)
                {
                    return false;
                }
            }
            _fileEventTimestamps[filePath] = now;
            return true;
        }

        public async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!ShouldProcessEvent(e.FullPath))
            {
                Debug.WriteLine($"[FolderWatcher] Ignorowanie zdarzenia: {e.FullPath}");
                return;
            }

            const int maxFileSize = 4 * 1024 * 1024; // 4 MB

            Debug.WriteLine($"[FolderWatcher] Plik utworzony: {e.FullPath}");

            if (File.Exists(e.FullPath))
            {
                var fileInfo = new FileInfo(e.FullPath);
                double fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                double maxFileSizeMB = maxFileSize / (1024.0 * 1024.0);

                if (fileInfo.Length > maxFileSize)
                {
                    Debug.WriteLine($"[FolderWatcher] Plik jest za duży: {fileSizeMB:F2} MB. Maksymalny rozmiar to {maxFileSizeMB:F2} MB.");
                    MessageBox.Show($"Plik {e.FullPath} jest za duży ({fileSizeMB:F2} MB). Maksymalny rozmiar to {maxFileSizeMB:F2} MB.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    var fileContent = File.ReadAllBytes(e.FullPath);
                    var response = await _clientService.UploadFileAsync(e.FullPath, fileContent);

                    if (!response.Success)
                    {
                        MessageBox.Show(response.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (IOException ex)
                {
                    Debug.WriteLine($"[FolderWatcher] Nie można uzyskać dostępu do pliku: {ex.Message}");
                }
            }
        }

        public async void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (!ShouldProcessEvent(e.FullPath)) return;

            Debug.WriteLine($"[FolderWatcher] Plik usunięty: {e.FullPath}");

            string fileName = Path.GetFileName(e.FullPath);

            var response = await _clientService.DeleteFileAsync(fileName);

            if (!response.Success)
            {
                MessageBox.Show(response.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (!ShouldProcessEvent(e.FullPath)) return;

            Debug.WriteLine($"[FolderWatcher] Plik zmieniony z {e.OldFullPath} na {e.FullPath}");

            string oldFileName = Path.GetFileName(e.OldFullPath);

            var deleteResponse = await _clientService.DeleteFileAsync(oldFileName);
            if (deleteResponse.Success)
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

        public async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!ShouldProcessEvent(e.FullPath)) return;

            Debug.WriteLine($"[FolderWatcher] Plik zmieniony: {e.FullPath}");

            try
            {
                if (File.Exists(e.FullPath))
                {
                    var fileContent = File.ReadAllBytes(e.FullPath);

                    var uploadResponse = await _clientService.UploadFileAsync(e.FullPath, fileContent);

                    if (!uploadResponse.Success)
                    {
                        MessageBox.Show(uploadResponse.Message, "Błąd przesyłania pliku", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        Debug.WriteLine($"[FolderWatcher] Plik pomyślnie przesłany: {e.FullPath}");
                    }
                }
                else
                {
                    Debug.WriteLine($"[FolderWatcher] Plik nie istnieje: {e.FullPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas obsługi zmiany pliku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileCreated;
                _watcher.Deleted -= OnFileDeleted;
                _watcher.Renamed -= OnFileRenamed;
                _watcher.Changed -= OnFileChanged;
                _watcher.Dispose();
                Debug.WriteLine("[FolderWatcher] Watcher został zwolniony.");
            }
        }
    }
}
