using LocalShare.Desktop.FileHandler;
using LocalShare.Protocol.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.KeepStates
{
    public class ReceiveDataHolder
    {
        private readonly Dictionary<string, ReceiveFileHandler> _caches = new();




        public void AddReceiveFileHandler(PreStartFileTaskRequest req)
        {
            if (!_caches.ContainsKey(req.TaskId))
            {
                _caches.Add(req.TaskId, new ReceiveFileHandler
                {
                    SendNodeIp = req.SendNodeIp,
                    TaskId = req.TaskId,
                    SendNodeName = req.SendNodeName
                });
            }
        }

        public ReceiveFileHandler? GetReceiveFileHandler(string taskId)
        {
            _caches.TryGetValue(taskId, out var val);
            return val;
        }

    }
}
