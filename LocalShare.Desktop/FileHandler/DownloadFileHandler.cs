using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using LocalShare.Desktop.Models;
using LocalShare.Desktop.Models.Browsers;
using LocalShare.Desktop.Models.Sends;
using LocalShare.Protocol.Define;
using Serilog;
using System.IO;

namespace LocalShare.Desktop.FileHandler
{
    public class DownloadFileHandler
    {
        private readonly LocalNode _node;
        private CancellationTokenSource? _token;
        public DownloadFileHandler(LocalNode node, DownloadFileTaskItem item)
        {
            _node = node;
            DownloadItem = item;
        }

        public DownloadFileTaskItem? DownloadItem { get; private set; }
        private bool hasStop = false;
        private bool hasCancel = false;


        public void Stop()
        {
            hasStop = true;
            _token?.Cancel();
            if (DownloadItem != null)
            {
                DownloadItem.State = 2;
            }
        }

        public void Cancel()
        {
            hasCancel = true;
            _token?.Cancel();
            if (DownloadItem != null)
            {
                DownloadItem.State = 4;
            }
        }

        public async Task Restart()
        {
            await HandleDownloadFile();
        }

        public async Task Start()
        {
            await HandleDownloadFile();
        }


        private async Task HandleDownloadFile()
        {
            AsyncServerStreamingCall<FileChunk>? response = null;
            FileStream? fs = null;
            _token = new CancellationTokenSource();
            try
            {
                var channel = _node.GetRpcChannel();
                if (channel != null && DownloadItem != null)
                {
                    var filePath = Path.Combine(GlobalShared.SaveFilePath, DownloadItem.FileName);
                    long startByteIndex = 0;
                    if (File.Exists(filePath))
                    {
                        FileInfo fi = new FileInfo(filePath);
                        startByteIndex = fi.Length;
                        DownloadItem.CurrentSize = fi.Length;
                        fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
                    }
                    else
                    {
                        fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    }
                    var request = new DownloadFileRequest
                    {
                        FileName = DownloadItem.FileName,
                        StartByteIndex = startByteIndex,
                        DownloadSpeed = GlobalShared.DownloadSpeed
                    };
                    var client = new LocalShareService.LocalShareServiceClient(channel);
                    response = client.DownloadFile(request);
                    DownloadItem.State = 1;
                    while (await response.ResponseStream.MoveNext(_token.Token))
                    {
                        var eachPart = response.ResponseStream.Current.Data.ToByteArray();
                        await fs.WriteAsync(eachPart, _token.Token);
                        DownloadItem.CurrentSize += eachPart.Length;
                        if (_token.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    if (DownloadItem.TotalSize == DownloadItem.CurrentSize)
                    {
                        DownloadItem.State = 3;
                        WeakReferenceMessenger.Default.Send(new MessageModel
                        {
                            MessageType = MessageType.FinishDownloadFile,
                            Data = DownloadItem
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (DownloadItem != null && !hasCancel && !hasStop)
                {
                    DownloadItem.State = 4;
                }
                WeakReferenceMessenger.Default.Send(new MessageModel
                {
                    MessageType = MessageType.DownloadFileError,
                    Data = DownloadItem
                });
                Log.Error($"Download file error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                hasStop = false;
                hasCancel = false;
                response?.Dispose();
                if (fs != null)
                {
                    await fs.DisposeAsync();
                }
            }
        }
    }
}
