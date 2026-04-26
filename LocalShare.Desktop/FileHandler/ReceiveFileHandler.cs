using Grpc.Core;
using HandyControl.Controls;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.DataContext.Entities;
using LocalShare.Desktop.Models.Receives;
using LocalShare.Protocol.Define;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Grpc.Core.Metadata;

namespace LocalShare.Desktop.FileHandler
{
    public class ReceiveFileHandler
    {
        private ReceiveFileTaskEntity? entity;

        public string TaskId { get; set; } = string.Empty;
        public string SendNodeName { get; set; } = string.Empty;
        public string SendNodeIp { get; set; } = string.Empty;

        public ReceiveFileTaskModel? TaskModel { get; set; }


        public async Task<StartFileTaskResponse> HandleStartFileTask(StartFileTaskRequest req)
        {
            var result = new StartFileTaskResponse
            {
                TaskId = req.FileMetaData.TaskId,
                FileName = req.FileMetaData.FileName,
                Status = FileTaskStatus.Success
            };

            using var dbContext = new LocalDataContext();
            entity = await dbContext.ReceiveFileTasks.AsNoTracking().FirstOrDefaultAsync(s => s.TaskId == req.FileMetaData.TaskId);
            if (entity == null)
            {
                entity = new ReceiveFileTaskEntity
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
                    entity.FileFullName = Path.Combine(GlobalShared.DownloadPath!, req.FileMetaData.RelativePath);
                }
                await dbContext.AddAsync(entity);
                await dbContext.SaveChangesAsync();
            }

            if (File.Exists(entity.FileFullName))
            {
                //File.Delete(existedItem.FileFullName);
                FileInfo fi = new FileInfo(entity.FileFullName);
                result.StartByteIndex = fi.Length;
            }

            TaskModel = new ReceiveFileTaskModel
            {
                FileName = entity.FileName,
                TotalSize = req.FileMetaData.TotalSize,
                TaskId = entity.TaskId,
                SaveFilePath = entity.FileFullName,
                SendNodeName = entity.SendNodeName,
                State = 1,
                CurrentSize = result.StartByteIndex,
            };

            return result;
        }

        public async Task HandleFileTask(IAsyncStreamReader<FileChunk> request)
        {
            FileStream? fs = null;
            try
            {
                if (entity != null)
                {
                    if (File.Exists(entity.FileFullName))
                    {
                        fs = new FileStream(entity.FileFullName, FileMode.Append, FileAccess.Write);
                    }
                    else
                    {
                        FileInfo fi = new FileInfo(entity.FileFullName);
                        if (!string.IsNullOrEmpty(fi.DirectoryName) &&
                             !Directory.Exists(fi.DirectoryName))
                        {
                            Directory.CreateDirectory(fi.DirectoryName);
                        }
                        fs = new FileStream(entity.FileFullName, FileMode.Create, FileAccess.Write);
                    }
                    while (await request.MoveNext())
                    {
                        await fs.WriteAsync(request.Current.Data.ToByteArray());
                        TaskModel!.CurrentSize += request.Current.Data.Length;
                    }

                    if (entity != null)
                    {
                        entity.State = 3;
                        entity.LastUpdateTime = DateTime.UtcNow;
                        TaskModel!.State = 3;
                    }
                }
            }
            catch (Exception ex)
            {
                if (entity != null)
                {
                    entity.State = 4;
                    TaskModel!.State = 4;
                }
                Log.Error($"HandleFileTask error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                fs?.Dispose();
                using var dbContext = new LocalDataContext();
                dbContext.ReceiveFileTasks.Update(entity!);
                await dbContext.SaveChangesAsync();
                Growl.Info($"文件接收完成，文件名：{entity!.FileName}");
            }
        }
    }
}
