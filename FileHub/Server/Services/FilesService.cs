﻿using Common.GRPC;
using Common.Converters;
using Grpc.Net.Client;
using Common;

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
        string creationDate = FormatConverter.SanitizeFileName(DateTimeConverter.ConvertToDateTime(request.CreationDate).ToString("yyyyMMdd-HHmmss"));
        var fileName = $"{request.FileName}~{request.FileType}~{request.UserId}~{creationDate}~{Guid.NewGuid()}";
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
        var userFiles = new List<FileData>();

        var localFiles = Directory.GetFiles(_path)
                               .Where(file => file.Contains($"~{request.UserId}~"))
                               .ToList();

        foreach (var filePath in localFiles)
        {
            var fileInfo = new FileInfo(filePath);
            var fileData = await FormatConverter.DecodeFileDataFromName(fileInfo);
            userFiles.Add(fileData);
        }

        var nodes = _dhtService.GetNodes();

        foreach (var node in nodes)
        {
            if(node.Address == AppSettings.DefaultAddress && node.Port == _dhtService.GetServerPort()) continue; //Pomijanie aktualnego węzła

            // Pobierz pliki od innego serwera
            var channel = GrpcChannel.ForAddress($"http://{node.Address}:{node.Port}");
            var client = new DistributedFileServer.DistributedFileServerClient(channel);

            var remoteResponse = await client.DownloadLocalFilesAsync(request);
            if (remoteResponse.Success && !remoteResponse.Message.Contains("No files found"))
            {
                userFiles.AddRange(remoteResponse.Files);
            }
        }

        //userFiles = response.Files.GroupBy(f => f.FileName).Select(g => g.First()).ToList();
        if (userFiles.Any())
        {
            response.Success = true;
            response.Files.AddRange(userFiles);
        }
        else
        {
            response.Success = true;
            response.Message = "No files found for this user.";
        }

        return response;
    }

    public async Task<DownloadResponse> GetLocalUserFiles(DownloadRequest request)
    {
        var response = new DownloadResponse();
        var userFiles = new List<FileData>();
        
        var localFiles = Directory.GetFiles(_path)
                               .Where(file => file.Contains($"~{request.UserId}~"))
                               .ToList();

        foreach (var filePath in localFiles)
        {
            var fileInfo = new FileInfo(filePath);
            var fileData = await FormatConverter.DecodeFileDataFromName(fileInfo);
            userFiles.Add(fileData);
        }

        if (userFiles.Any())
        {
            response.Success = true;
            response.Files.AddRange(userFiles);
        }
        else
        {
            response.Success = true;
            response.Message = "No files found for this user.";
        }
        return response;
    }

    public async Task<DownloadResponse> GetUserFiles(DeleteRequest request)
    {
        var response = new DownloadResponse();
        var userFiles = new List<FileData>();

        var localFiles = Directory.GetFiles(_path)
                               .Where(file => file.Contains($"~{request.UserId}~"))
                               .ToList();

        var currentServerPort = _dhtService.GetServerPort();

        foreach (var filePath in localFiles)
        {
            var fileInfo = new FileInfo(filePath);
            var fileData = await FormatConverter.DecodeFileDataFromName(fileInfo);
            fileData.ServerId = currentServerPort;
            userFiles.Add(fileData);
        }

        var nodes = _dhtService.GetNodes();

        foreach (var node in nodes)
        {
            if (node.Address == AppSettings.DefaultAddress && node.Port == _dhtService.GetServerPort()) continue; //Pomijanie aktualnego węzła

            // Pobierz pliki od innego serwera
            var channel = GrpcChannel.ForAddress($"http://{node.Address}:{node.Port}");
            var client = new DistributedFileServer.DistributedFileServerClient(channel);

            var downloadRequest = new DownloadRequest
            {
                UserId = request.UserId
            };

            var remoteResponse = await client.DownloadLocalFilesAsync(downloadRequest);
            if (remoteResponse.Success)
            {
                foreach(var file in remoteResponse.Files)
                {
                    file.ServerId = node.Port;
                }

                userFiles.AddRange(remoteResponse.Files);
            }
        }

        if (userFiles.Any())
        {
            response.Success = true;
            response.Files.AddRange(userFiles);
        }
        else
        {
            response.Success = false;
            response.Message = "No files found for this user.";
        }

        return response;
    }
}