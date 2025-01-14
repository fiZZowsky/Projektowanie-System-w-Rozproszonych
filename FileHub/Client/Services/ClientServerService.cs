﻿using Grpc.Core;

namespace Client.Services
{
    public class ClientServerService : Common.GRPC.DistributedFileServer.DistributedFileServerBase
    {
        private ClientService _clientService;
        public ClientServerService(ClientService clientService)
        {
            _clientService = clientService;
        }

        public override async Task<Common.GRPC.TransferResponse> TransferFile(Common.GRPC.TransferRequest request, ServerCallContext context)
        {
            try
            {
                await _clientService.SyncFileFromServerAsync(request);
                return new Common.GRPC.TransferResponse { Success = true, Message = "Pomyślnie odebrano plik od serwera." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file transfer: {ex.Message}");
                return new Common.GRPC.TransferResponse { Success = false };
            }
        }
    }
}
