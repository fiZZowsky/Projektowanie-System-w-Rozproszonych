using Common.GRPC;
using Common.Converters;
using Grpc.Net.Client;

namespace Server.Services;

public class FilesService
{
    private readonly string _path;

    public FilesService(string filesDirectoryPath)
    {
        _path = filesDirectoryPath;
    }

    public async Task<UploadResponse> SaveFile(UploadRequest request)
    {
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
