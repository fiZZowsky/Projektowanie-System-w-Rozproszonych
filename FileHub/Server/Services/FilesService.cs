using Common.GRPC;
using Common.Converters;
using Server.Utils;
using Grpc.Net.Client;

namespace Server.Services;

public class FilesService
{
    private readonly string _path;
    private readonly DHTService _dhtService;

    public FilesService(string filesDirectoryPath, DHTService dhtService)
    {
        _path = filesDirectoryPath;
        _dhtService = dhtService;
    }

    public async Task<UploadResponse> SaveFile(UploadRequest request)
    {
        var availableNodes = _dhtService.GetNodes();
        var node = DHTManager.FindResponsibleNode(request.FileName, availableNodes);
        if(node.Port != AppConfig.MulticastPort)
        {
            bool success = await SendFileToServer(request, node.Address, node.Port);
            if (success)
            {
                return new UploadResponse { Success = true, Message = "File uploaded to the responsible server." };
            }
            else
            {
                return new UploadResponse { Success = false, Message = "Failed to upload file to responsible server." };
            }
        }

        string creationDate = FormatConverter.SanitizeFileName(DateTimeConverter.ConvertToDateTime(request.CreationDate).ToString("yyyyMMdd-HHmmss"));
        var fileName = $"{request.FileName}_{request.FileType}_{request.UserId}_{creationDate}_{Guid.NewGuid()}";
        var filePath = Path.Combine(_path, fileName);

        await File.WriteAllBytesAsync(filePath, request.FileContent.ToByteArray());


        return new UploadResponse { Success = true, Message = "File uploaded successfully." };
    }

    public async Task<bool> SendFileToServer(UploadRequest request, string targetAddress, int targetPort)
    {
        var channel = GrpcChannel.ForAddress($"http://{targetAddress}:{targetPort}");
        var client = new DistributedFileServer.DistributedFileServerClient(channel);

        var response = await client.UploadFileAsync(request);
        return response.Success;
    }


    public async Task<DownloadResponse> GetUserFiles(DownloadRequest request)
    {
        var response = new DownloadResponse();
        var userFiles = new List<FileInfo>();

        var files = Directory.GetFiles(_path)
                                     .Where(file => file.Contains(request.UserId))
                                     .ToList();

        if (files.Any())
        {
            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);
                userFiles.Add(fileInfo);
            }

            response.Success = true;
            foreach (var fileInfo in userFiles)
            {
                var fileData = await FormatConverter.DecodeFileDataFromName(fileInfo);

                response.Files.Add(fileData);
            }
        }
        else
        {
            response.Success = true;
            response.Message = "No files found for this user.";
        }

        return response;
    }
}
