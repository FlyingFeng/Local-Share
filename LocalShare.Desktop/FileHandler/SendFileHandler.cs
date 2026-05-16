using Grpc.Core;
using HandyControl.Controls;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.DataContext.Entities;
using LocalShare.Desktop.Models.Sends;
using LocalShare.Protocol.Define;
using Serilog;
using System.Buffers;
using System.IO;

namespace LocalShare.Desktop.FileHandler
{
    public class SendFileHandler
    {
        private readonly string _receiveNodeName;
        private readonly Channel _channel;
        private readonly LocalShareService.LocalShareServiceClient _client;

        private readonly int eachReadBytes; //1024 * 256; //256kb
        private readonly LocalNode _node;
        private readonly CancellationTokenSource _tokenSource;

        public SendFileHandler(Channel channel,
            string receiveNodeName,
            LocalNode node)
        {
            eachReadBytes = GlobalShared.TransferSpeed * 1024;
            _node = node;
            _channel = channel;
            _client = new LocalShareService.LocalShareServiceClient(_channel);
            _receiveNodeName = receiveNodeName;
            _tokenSource = new CancellationTokenSource();
        }

        public bool HasError { get; set; }


        public void CancelFileTask()
        {
            _tokenSource?.Cancel();
        }


        public async Task SendFile(FileTaskModel model, SendFileTaskEntity entity)
        {
            using var dbContext = new LocalDataContext();
            try
            {
                await dbContext.AddAsync(entity);
                await dbContext.SaveChangesAsync();
                if (File.Exists(model.FullFileName))
                {
                    FileInfo fi = new FileInfo(model.FullFileName);
                    await PreStartFileTask(model);
                    var response = await StartFileTask(fi, model);
                    await ReadAndSendFile(fi, response, model);
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                model.State = 4;
                Log.Error($"SendFileHandler.SendFile error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _node.FinishSendFile(model);
            }
            try
            {
                entity.LastUpdateTime = DateTime.UtcNow;
                if (HasError)
                {
                    entity.State = 4;
                }
                else
                {
                    entity.State = 3;
                }
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"SendFileHandler.db error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                if (HasError)
                {
                    Growl.Info($"文件发送失败，文件名：{model.FileName}");
                }
                else
                {
                    Growl.Info($"文件发送完成，文件名：{model.FileName}");
                }
            }
            //finally
            //{
            //    using var dbContext = new LocalDataContext();
            //    entity.LastUpdateTime = DateTime.UtcNow;
            //    if (flag)
            //    {
            //        entity.State = 3;
            //    }
            //    else
            //    {
            //        entity.State = 4;
            //    }
            //    dbContext.SendFileTasks.Update(entity);
            //    await dbContext.SaveChangesAsync();
            //}
        }


        private async Task ReadAndSendFile(FileInfo fi, StartFileTaskResponse fileTask, FileTaskModel model)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(eachReadBytes);
            AsyncClientStreamingCall<FileChunk, EmptyMessage>? request = null;
            try
            {
                Metadata header = new Metadata
                {
                    { "task_id", fileTask.TaskId }
                };

                request = _client.SendFile(header, cancellationToken: _tokenSource.Token);
                using FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
                fs.Position = fileTask.StartByteIndex;
                model.CurrentSize += fileTask.StartByteIndex;
                while (true)
                {
                    int read = await fs.ReadAsync(buffer, cancellationToken: _tokenSource.Token);
                    if (read <= 0)
                    {
                        break;
                    }
                    if (_tokenSource.IsCancellationRequested)
                    {
                        HasError = true;
                        break;
                    }
                    var chunkData = new FileChunk
                    {
                        Data = Google.Protobuf.ByteString.CopyFrom(buffer, 0, read)
                    };
                    await request.RequestStream.WriteAsync(chunkData);
                    model.CurrentSize += read;
                }
                if (model.CurrentSize == model.TotalSize)
                {
                    model.State = 3;
                }
                //await Task.Delay(3000, _tokenSource.Token);
            }
            catch (Exception ex)
            {
                HasError = true;
                Log.Error($"ReadAndSendFile error, {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                if (request != null)
                {
                    await request.RequestStream.CompleteAsync();
                    request.Dispose();
                }
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
            var response = await _client.PreStartFileTaskAsync(request, cancellationToken: _tokenSource.Token);
            return response.Status;
        }


        private async Task<StartFileTaskResponse> StartFileTask(FileInfo fi, FileTaskModel model)
        {
            string relativeName = string.Empty;
            if (fi.Directory != null && model.IsOpenFromDir)
            {
                relativeName = fi.FullName.Split(fi.Directory.Root.Name)[1];
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
            var response = await _client.StartFileTaskAsync(request, cancellationToken: _tokenSource.Token);
            return response;
        }


    }
}
