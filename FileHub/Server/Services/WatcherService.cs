using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Server.Services
{
    public class WatcherService
    {
        private FileSystemWatcher _watcher;
        private readonly string _path;
        private readonly MulticastService _multicastService;

        public WatcherService(string path)
        {
            _path = path;
            _multicastService = new MulticastService();
        }

        public void StartWatching()
        {
            _watcher = new FileSystemWatcher
            {
                Path = _path,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                Filter = "*.*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"[FileWatcher] File created: {e.FullPath}");
            _multicastService.AnnounceFileChange(e.FullPath, "ADD");
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"[FileWatcher] File deleted: {e.FullPath}");
            _multicastService.AnnounceFileChange(e.FullPath, "DELETE");
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"[FileWatcher] File renamed from {e.OldFullPath} to {e.FullPath}");
            _multicastService.AnnounceFileChange(e.OldFullPath, "DELETE");
            _multicastService.AnnounceFileChange(e.FullPath, "ADD");
        }
    }
}
