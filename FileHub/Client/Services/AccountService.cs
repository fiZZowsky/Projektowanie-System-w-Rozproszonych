using Commm.Converters;
using Common.Models;
using Grpc.Net.Client;
using static Common.GRPC.DistributedFileServer;

namespace Client.Services;

public class AccountService
{
    public AccountService()
    {
    }

    public async Task<Common.GRPC.UserDataResponse> CreateNewUserAsync(string username, string password, NodeInfo node)
    {
        var encryptedPassword = PasswordEncryptor.EncryptPassword(password);

        if (string.IsNullOrWhiteSpace(encryptedPassword)) return new Common.GRPC.UserDataResponse { Success = false, Message = "Encountered an error during password encryption." };

        using var channel = GrpcChannel.ForAddress($"http://{node.Address}:{node.Port}");
        var client = new DistributedFileServerClient(channel);

        var userDataRequest = new Common.GRPC.UserDataRequest
        {
            Username = username,
            PasswordHash = encryptedPassword
        };

        var response = await client.RegisterNewUserAsync(userDataRequest);
        return response;
    }

    public async Task<Common.GRPC.UserDataResponse> LoginUserAsync(string username, string password, NodeInfo node)
    {
        var encryptedPassword = PasswordEncryptor.EncryptPassword(password);

        if (string.IsNullOrWhiteSpace(encryptedPassword)) return new Common.GRPC.UserDataResponse { Success = false, Message = "Encountered an error during password encryption." };

        using var channel = GrpcChannel.ForAddress($"http://{node.Address}:{node.Port}");
        var client = new DistributedFileServerClient(channel);

        var userDataRequest = new Common.GRPC.UserDataRequest
        {
            Username = username,
            PasswordHash = encryptedPassword
        };

        var response = await client.LoginUserAsync(userDataRequest);
        return response;
    }
}