using Grpc.Core;
using LocalShare.Desktop.DataContext;
using LocalShare.Protocol.Define;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.Server
{
    internal class LocalServer : LocalShare.Protocol.Define.LocalShareService.LocalShareServiceBase
    {
        //private readonly IServiceProvider _serviceProvider;
        //public LocalServer(IServiceProvider serviceProvider)
        //{
        //    _serviceProvider = serviceProvider;
        //}

        public override Task<PreStartFileTaskResponse> PreStartFileTask(PreStartFileTaskRequest request, ServerCallContext context)
        {
            return Task.FromResult(new PreStartFileTaskResponse
            {
                NeedPassword = false,
                Status = FileTaskStatus.Success
            });
        }

        public override Task<StartFileTaskResponse> StartFileTask(StartFileTaskRequest request, ServerCallContext context)
        {
            return base.StartFileTask(request, context);
        }

        public override Task<EmptyMessage> SendFile(IAsyncStreamReader<FileChunk> requestStream, ServerCallContext context)
        {
            return base.SendFile(requestStream, context);
        }

        public override Task DownloadFile(EmptyMessage request, IServerStreamWriter<FileChunk> responseStream, ServerCallContext context)
        {
            return base.DownloadFile(request, responseStream, context);
        }


        public override Task<NodeModel> GetServerNodeInfo(EmptyMessage request, ServerCallContext context)
        {
            try
            {
                using var _dbContext = new LocalDataContext();
                var entity = _dbContext!.LocalNodes.FirstOrDefault();
                if (entity != null)
                {
                    return Task.FromResult(new NodeModel
                    {
                        IpAddress = GlobalShared.IpAddress,
                        MachineName = Environment.MachineName,
                        NodeName = entity.NodeName,
                        Port = GlobalShared.ServerPort,
                        Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"LocalServer.GetServerNodeInfo error, {ex.Message}\n{ex.StackTrace}");
            }
            throw new Exception("Not found node");
        }

    }
}
