using Grpc.Core;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.DataContext.Entities;
using LocalShare.Desktop.Models.Sends;
using LocalShare.Protocol.Define;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.FileHandler
{
    public class SendFileHandler
    {
        private readonly string _receiveIpAddress;
        private readonly string _receiveNodeName;
        private readonly Channel _channel;
        private readonly LocalShareService.LocalShareServiceClient _client;
        //private readonly IServiceProvider _serviceProvider;

        private readonly int eachReadBytes = 1024 * 256; //256kb
        private readonly LocalNode _node;

        public SendFileHandler(Channel channel,
            string receiveIpAddress,
            string receiveNodeName,
            LocalNode node)
        {
            _node = node;
            _channel = channel;
            _client = new LocalShareService.LocalShareServiceClient(_channel);
            _receiveIpAddress = receiveIpAddress;
            _receiveNodeName = receiveNodeName;
        }


        public async Task SendFile(FileTaskModel model)
        {
            using var dbContext = new LocalDataContext();
            SendFileTaskEntity? entity = null;
            try
            {
                if (File.Exists(model.FullFileName))
                {
                    FileInfo fi = new FileInfo(model.FullFileName);
                    await PreStartFileTask(model);
                    var response = await StartFileTask(fi, model);
                    await ReadAndSendFile(fi, response, model);
                    await _node.FinishSendFile(model);
                }
            }
            catch (Exception ex)
            {
                if (entity != null)
                {
                    entity.State = 4;
                    dbContext!.SendFileTasks.Update(entity);
                    await dbContext!.SaveChangesAsync();
                }

                Log.Error($"SendFileHandler.SendFile error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {

            }
        }


        private async Task ReadAndSendFile(FileInfo fi, StartFileTaskResponse fileTask, FileTaskModel model)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(eachReadBytes);
            try
            {
                Metadata header = new Metadata
                {
                    { "task_id", fileTask.TaskId }
                };

                var request = _client.SendFile(header);
                using FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
                fs.Position = fileTask.StartByteIndex;
                while (true)
                {
                    int read = await fs.ReadAsync(buffer);
                    if (read <= 0)
                    {
                        break;
                    }
                    var chunkData = new FileChunk
                    {
                        Data = Google.Protobuf.ByteString.CopyFrom(buffer, 0, read)
                    };
                    await request.RequestStream.WriteAsync(chunkData);
                    model.CurrentSize += read;
                }
                await request.RequestStream.CompleteAsync();
                request.Dispose();
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task<FileTaskStatus> PreStartFileTask(FileTaskModel model)
        {
            var request = new PreStartFileTaskRequest
            {
                ReceiveNodeName = _receiveNodeName,
                SendNodeName = GlobalShared.NodeName!,
                TaskId = model.TaskId,
                SendNodeIp = GlobalShared.IpAddress!
            };
            var response = await _client.PreStartFileTaskAsync(request);
            return response.Status;
        }


        private async Task<StartFileTaskResponse> StartFileTask(FileInfo fi, FileTaskModel model)
        {
            string relativeName = string.Empty;
            if (fi.Directory != null && model.IsOpenFromDir)
            {
                relativeName = fi.FullName.Split(fi.Directory.Root.Name)[1];
            }
            int totalChunk = (int)(fi.Length / eachReadBytes);
            if (fi.Length % eachReadBytes != 0)
            {
                totalChunk += 1;
            }

            var request = new StartFileTaskRequest
            {
                Password = "",
                StartByteIndex = 0,
                FileMetaData = new FileMetaData
                {
                    ChunkSize = eachReadBytes,  //256kb
                    FileExt = fi.Extension,
                    FileName = fi.Name,
                    Md5 = model.Md5,
                    TotalSize = fi.Length,
                    RelativePath = relativeName,
                    TaskId = model.TaskId
                }
            };
            var response = await _client.StartFileTaskAsync(request);
            return response;
        }





    }
}
