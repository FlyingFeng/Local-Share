using Grpc.Core;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.DataContext.Entities;
using LocalShare.Protocol.Define;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.FileHandler
{
    public class ReceiveFileHandler
    {
        private ReceiveFileTaskEntity? existedItem;

        public string TaskId { get; set; } = string.Empty;
        public string SendNodeName { get; set; } = string.Empty;
        public string SendNodeIp { get; set; } = string.Empty;

        public async Task<StartFileTaskResponse> HandleStartFileTask(StartFileTaskRequest req)
        {
            var result = new StartFileTaskResponse
            {
                TaskId = req.FileMetaData.TaskId,
                FileName = req.FileMetaData.FileName,
                Status = FileTaskStatus.Success
            };

            using var dbContext = new LocalDataContext();
            existedItem = await dbContext.ReceiveFileTasks.AsNoTracking().FirstOrDefaultAsync(s => s.TaskId == req.FileMetaData.TaskId);
            if (existedItem == null)
            {
                existedItem = new ReceiveFileTaskEntity
                {
                    FileName = req.FileMetaData.FileName,
                    InitTime = DateTime.UtcNow,
                    LastUpdateTime = DateTime.UtcNow,
                    TaskId = req.FileMetaData.TaskId,
                    ReceiveIpAddress = GlobalShared.IpAddress!,
                    ReceiveNodeName = GlobalShared.NodeName!,
                    SendIpAddress = SendNodeIp,
                    SendNodeName = SendNodeName,
                    State = 1,
                    FileFullName = Path.Combine(GlobalShared.DownloadPath!, req.FileMetaData.FileName)
                };
                if (!string.IsNullOrWhiteSpace(req.FileMetaData.RelativePath))
                {
                    existedItem.FileFullName = Path.Combine(GlobalShared.DownloadPath!, req.FileMetaData.RelativePath);
                }
                await dbContext.AddAsync(existedItem);
                await dbContext.SaveChangesAsync();
            }

            if (File.Exists(existedItem.FileFullName))
            {
                File.Delete(existedItem.FileFullName);
                //FileInfo fi = new FileInfo(existedItem.FileFullName);
                //result.StartByteIndex = fi.Length;
            }

            return result;
        }

        public async Task HandleFileTask(IAsyncStreamReader<FileChunk> request)
        {
            FileStream? fs = null;
            try
            {
                if (existedItem != null)
                {
                    if (File.Exists(existedItem.FileFullName))
                    {
                        fs = new FileStream(existedItem.FileFullName, FileMode.Append, FileAccess.Write);
                    }
                    else
                    {
                        FileInfo fi = new FileInfo(existedItem.FileFullName);
                        if (!string.IsNullOrEmpty(fi.DirectoryName) &&
                             !Directory.Exists(fi.DirectoryName))
                        {
                            Directory.CreateDirectory(fi.DirectoryName);
                        }
                        fs = new FileStream(existedItem.FileFullName, FileMode.Create, FileAccess.Write);
                    }
                    while (await request.MoveNext())
                    {
                        await fs.WriteAsync(request.Current.Data.ToByteArray());
                    }

                    using var dbContext = new LocalDataContext();
                    var item = dbContext.ReceiveFileTasks.FirstOrDefault(s => s.TaskId == existedItem.TaskId);
                    if (item != null)
                    {
                        item.State = 3;
                        item.LastUpdateTime = DateTime.UtcNow;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"HandleFileTask error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                fs?.Dispose();
            }
        }
    }
}
