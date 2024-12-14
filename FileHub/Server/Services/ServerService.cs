using Common.GRPC;
using Grpc.Core;

namespace Server.Services
{
    public class ServerService : DistributedFileServer.DistributedFileServerBase
    {
        private readonly string _path;
        private FilesService _filesService;

        public ServerService(string filesDirectoryPath)
        {
            _path = filesDirectoryPath;
            _filesService = new FilesService(filesDirectoryPath);
        }

        public override async Task<UploadResponse> UploadFile(UploadRequest request, ServerCallContext context)
        {
            try
            {
                Console.WriteLine($"[Upload] [{request.CreationDate}] File received: {request.FileName}.{request.FileType}");
                var response = await _filesService.SaveFile(request);
                Console.WriteLine($"[Upload] {request.FileName}.{request.FileType} Status successed: {response.Success}");
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
    }
}
