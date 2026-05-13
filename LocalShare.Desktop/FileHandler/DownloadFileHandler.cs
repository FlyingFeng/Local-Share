using Google.Protobuf;
using Grpc.Core;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.Models.Browsers;
using LocalShare.Desktop.Models.Sends;
using LocalShare.Protocol.Define;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        public void Stop()
        {
            _token?.Cancel();
            DownloadItem!.State = 2;
        }

        public void Cancel()
        {
            _token?.Cancel();
            DownloadItem!.State = 4;
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
                if (channel != null)
                {
                    var filePath = Path.Combine(GlobalShared.DownloadPath, DownloadItem!.FileName);
                    long startByteIndex = 0;
                    if (File.Exists(filePath))
                    {
                        FileInfo fi = new FileInfo(filePath);
                        startByteIndex = fi.Length;
                        DownloadItem!.CurrentSize = fi.Length;
                        fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
                    }
                    else
                    {
                        fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    }
                    var request = new DownloadFileRequest
                    {
                        FileName = DownloadItem.FileName,
                        StartByteIndex = startByteIndex
                    };
                    var client = new LocalShareService.LocalShareServiceClient(channel);
                    response = client.DownloadFile(request);
                    DownloadItem!.State = 1;
                    while (await response.ResponseStream.MoveNext(_token.Token))
                    {
                        var eachPart = response.ResponseStream.Current.Data.ToByteArray();
                        await fs.WriteAsync(eachPart, _token.Token);
                        DownloadItem!.CurrentSize += eachPart.Length;
                        if (_token.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    if (DownloadItem!.TotalSize == DownloadItem!.CurrentSize)
                    {
                        DownloadItem!.State = 3;
                    }
                    else
                    {
                        DownloadItem!.State = 4;
                    }
                }
            }
            catch (Exception ex)
            {
                DownloadItem!.State = 4;
                Log.Error($"Download file error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                response?.Dispose();
                if (fs != null)
                {
                    await fs.DisposeAsync();
                }
            }
        }
    }
}
