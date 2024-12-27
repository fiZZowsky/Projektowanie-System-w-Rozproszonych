using Common.GRPC;
using Grpc.Core;
using Server.Utils;

namespace Server.Services
{
    public class ServerService : DistributedFileServer.DistributedFileServerBase
    {
        private readonly string _path;
        private readonly DHTService _dhtService;
        private FilesService _filesService;

        public ServerService(string filesDirectoryPath, DHTService dhtService)
        {
            _path = filesDirectoryPath;
            _dhtService = dhtService;
            _filesService = new FilesService(filesDirectoryPath, _dhtService);
        }

        public override async Task<UploadResponse> UploadFile(UploadRequest request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"[Upload] [{request.CreationDate}] File received: {request.FileName}.{request.FileType}");

                var availableNodes = _dhtService.GetNodes();
                var node = DHTManager.FindResponsibleNode(request.FileName, availableNodes);

                if (node.Port != AppConfig.MulticastPort) // Jeśli inny serwer odpowiada za plik
                {
                    Console.WriteLine($"[Redirect] Forwarding upload to server at {node.Address}:{node.Port}");
                    bool success = await _filesService.SendFileToServer(request, node.Address, node.Port);

                    return new UploadResponse
                    {
                        Success = success,
                        Message = success
                            ? "File forwarded and uploaded successfully to the responsible server."
                            : "Failed to forward and upload the file to the responsible server."
                    };
                }

                var response = await _filesService.SaveFile(request);
                Console.WriteLine($"[Upload] {request.FileName}.{request.FileType} Status succeeded: {response.Success}");
                return response;
            }
            catch (Exception ex)
            {
                return new UploadResponse
                {
                    Success = false,
                    Message = $"Error uploading file: {ex.Message}"
                };
            }
        }

        public override async Task<DownloadResponse> DownloadFile(DownloadRequest request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"[Download] Requested by: {request.UserId}");
                var response = await _filesService.GetUserFiles(request);
                Console.WriteLine($"[Download] {request.UserId} Status seccessed: {response.Success}");
                return response;
            }
            catch (Exception ex)
            {
                return new DownloadResponse
                {
                    Success = false,
                    Message = $"Error downloading files: {ex.Message}"
                };
            }
        }
        public override async Task<DeleteResponse> DeleteFile(DeleteRequest request, ServerCallContext context)
        {
            string filePath = Path.Combine(_path, request.FileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"[Serwer] Plik {request.FileName} został usunięty.");
                return new DeleteResponse { Success = true, Message = "Plik usunięty." };
            }
            else
            {
                return new DeleteResponse { Success = false, Message = "Plik nie znaleziony." };
            }
        }

    }
}
