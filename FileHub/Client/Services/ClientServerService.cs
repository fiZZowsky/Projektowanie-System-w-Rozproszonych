using Common.GRPC;
using Grpc.Core;

namespace Client.Services
{
    public class ClientServerService : Common.GRPC.DistributedFileServer.DistributedFileServerBase
    {
        private ClientService _clientService;
        private WatcherService _folderWatcher;
        public ClientServerService(ClientService clientService, WatcherService folderWatcher)
        {
            _clientService = clientService;
            _folderWatcher = folderWatcher;
        }

        public override async Task<Common.GRPC.TransferResponse> TransferFile(Common.GRPC.TransferRequest request, ServerCallContext context)
        {
            try
            {
                _folderWatcher._watcher.Created -= _folderWatcher.OnFileCreated;
                await _clientService.SyncFileFromServerAsync(request, null);
                _folderWatcher._watcher.Created += _folderWatcher.OnFileCreated;
                return new Common.GRPC.TransferResponse { Success = true, Message = "Pomyślnie odebrano plik od serwera." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file transfer: {ex.Message}");
                return new Common.GRPC.TransferResponse { Success = false };
            }
        }

        public override async Task<DeleteResponse> DeleteFile(DeleteRequest request, ServerCallContext context)
        {
            try
            {
                _folderWatcher._watcher.Deleted -= _folderWatcher.OnFileDeleted;
                await _clientService.SyncFileFromServerAsync(null, request);
                _folderWatcher._watcher.Deleted += _folderWatcher.OnFileDeleted;
                return new Common.GRPC.DeleteResponse { Success = true, Message = "Pomyślnie odebrano komendę delete od serwera." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file deletion command: {ex.Message}");
                return new Common.GRPC.DeleteResponse { Success = false };
            }
        }
    }
}
