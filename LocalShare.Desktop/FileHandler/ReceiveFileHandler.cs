using Grpc.Core;
using LocalShare.Desktop.DataContext;
using LocalShare.Protocol.Define;
using Microsoft.EntityFrameworkCore;
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
        //private readonly IAsyncStreamReader<FileChunk> _stream;

        //public ReceiveFileHandler(IAsyncStreamReader<FileChunk> stream)
        //{
        //    _stream = stream;
        //}

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
            var existedItem = await dbContext.ReceiveFileTasks.FirstOrDefaultAsync(s => s.TaskId == req.FileMetaData.TaskId);
            if (existedItem == null)
            {
                existedItem = new DataContext.Entities.ReceiveFileTaskEntity
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


            return result;
        }

        public void HandleFileTask()
        {

        }
    }
}
