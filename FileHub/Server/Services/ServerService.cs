using Common.GRPC;
using Grpc.Core;

namespace Server.Services
{
    public class ServerService : DistributedFileServer.DistributedFileServerBase
    {
        public override Task<UploadResponse> UploadFile(UploadRequest request, ServerCallContext context)
        {
            Console.WriteLine($"[Upload] File received: {request.FileName}");
            return Task.FromResult(new UploadResponse { Success = true });
        }

        public override Task<DownloadResponse> DownloadFile(DownloadRequest request, ServerCallContext context)
        {
            Console.WriteLine($"[Download] File requested: {request.FileName}");
            return Task.FromResult(new DownloadResponse
            {
                FileContent = Google.Protobuf.ByteString.CopyFrom(new byte[0]),
                Success = true
            });
        }

        public override Task<PingResponse> Ping(PingRequest request, ServerCallContext context)
        {
            Console.WriteLine("[Ping] Received ping request");
            return Task.FromResult(new PingResponse { Success = true });
        }
    }
}
