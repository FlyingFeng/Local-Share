using Grpc.Core;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.KeepStates;
using LocalShare.Protocol.Define;
using Serilog;
using System.Buffers;
using System.IO;

namespace LocalShare.Desktop.Server
{
    internal class LocalServer : LocalShareService.LocalShareServiceBase
    {
        private readonly ReceiveDataHolder _receiveDataHolder;
        private readonly SendDataHolder _sendDataHolder;
        public LocalServer(ReceiveDataHolder receiveDataHolder, SendDataHolder sendDataHolder)
        {
            _sendDataHolder = sendDataHolder;
            _receiveDataHolder = receiveDataHolder;
        }

        public override Task<CommonResponse> Chat(ChatRequest request, ServerCallContext context)
        {
            var node = _sendDataHolder.Nodes.FirstOrDefault(s => s.NodeName == request.SendNodeName &&
                                                                s.IpAddress == request.SendNodeIp);
            if (node != null)
            {
                node.ReceiveChatMessage(request);
            }

            return Task.FromResult(new CommonResponse { Success = true });
        }


        public override Task<CommonResponse> OperateFileTask(FileOperationRequest request, ServerCallContext context)
        {
            if (request.Sender == 0)
            {
                var handller = _receiveDataHolder.GetReceiveFileHandler(request.TaskId);
                if (handller != null)
                {
                    handller.CancelFileTask(request.TaskId, _receiveDataHolder);
                }
            }
            else if (request.Sender == 1)
            {

            }
            return Task.FromResult(new CommonResponse
            {
                Success = true
            });
        }

        public override Task<PreStartFileTaskResponse> PreStartFileTask(PreStartFileTaskRequest request, ServerCallContext context)
        {
            _receiveDataHolder.AddReceiveFileHandler(request);
            return Task.FromResult(new PreStartFileTaskResponse
            {
                NeedPassword = false,
                Status = FileTaskStatus.Success,
                TaskId = request.TaskId
            });
        }

        public override async Task<StartFileTaskResponse> StartFileTask(StartFileTaskRequest request, ServerCallContext context)
        {
            var handler = _receiveDataHolder.GetReceiveFileHandler(request.FileMetaData.TaskId);
            if (handler != null)
            {
                var response = await handler.HandleStartFileTask(request);
                _receiveDataHolder.NotifyReceiveFileTaskAdded(handler);
                return response;
            }
            return new StartFileTaskResponse
            {
                Status = FileTaskStatus.Failed
            };
        }

        public override async Task<EmptyMessage> SendFile(IAsyncStreamReader<FileChunk> requestStream, ServerCallContext context)
        {
            var entity = context.RequestHeaders.Get("task_id");
            if (entity != null)
            {
                var handler = _receiveDataHolder.GetReceiveFileHandler(entity.Value);
                if (handler != null)
                {
                    await handler.HandleFileTask(requestStream, _receiveDataHolder);
                    //_receiveDataHolder.RemoveReceiveFileHandler(entity.Value);
                }
            }
            return new EmptyMessage();
        }

        public override async Task DownloadFile(DownloadFileRequest request, IServerStreamWriter<FileChunk> responseStream, ServerCallContext context)
        {
            FileStream? fs = null;
            var buffer = ArrayPool<byte>.Shared.Rent(1024 * 256);
            try
            {
                var startIndex = request.StartByteIndex;
                var matchedFile = _sendDataHolder.GetFile(request.FileName);
                if (matchedFile != null && File.Exists(matchedFile.FilePath))
                {
                    fs = new FileStream(matchedFile.FilePath, FileMode.Open, FileAccess.Read);
                    fs.Position = startIndex;
                    while (true)
                    {
                        var read = await fs.ReadAsync(buffer);
                        if (read <= 0)
                        {
                            break;
                        }
                        var chunkData = new FileChunk
                        {
                            Data = Google.Protobuf.ByteString.CopyFrom(buffer, 0, read)
                        };
                        await responseStream.WriteAsync(chunkData);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"DownloadFile(server) error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                if (fs != null)
                {
                    await fs.DisposeAsync();
                }
            }

        }

        public override Task<FileItemInfoArray> ListFiles(EmptyMessage request, ServerCallContext context)
        {
            FileItemInfoArray result = new FileItemInfoArray()
            {
                Files = { }
            };
            try
            {
                var allFiles = _sendDataHolder.GetAllFiles();
                var data = new List<FileItemInfo>();
                foreach (var file in allFiles)
                {
                    if (File.Exists(file.FilePath))
                    {
                        var fi = new FileInfo(file.FilePath);
                        string relativeName = string.Empty;
                        if (fi.Directory != null && file.IsOpenFromDir)
                        {
                            relativeName = fi.FullName.Split(fi.Directory.Root.Name)[1];
                        }
                        var each = new FileItemInfo
                        {
                            FileExt = fi.Extension,
                            FileName = fi.Name,
                            TotalSize = fi.Length,
                            RelativePath = relativeName
                        };
                        data.Add(each);
                    }
                }
                result.Files.Add(data);
            }
            catch (Exception ex)
            {
                Log.Error($"ListFiles error, {ex.Message}\n{ex.StackTrace}");
            }
            return Task.FromResult(result);
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
