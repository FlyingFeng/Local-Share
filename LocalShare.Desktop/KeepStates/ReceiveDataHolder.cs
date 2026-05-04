using LocalShare.Desktop.FileHandler;
using LocalShare.Protocol.Define;

namespace LocalShare.Desktop.KeepStates
{
    public delegate void ReceiveFileTaskEventHandler(ReceiveFileHandler handler);

    public class ReceiveDataHolder
    {
        private readonly Dictionary<string, ReceiveFileHandler> _caches = new();

        public event ReceiveFileTaskEventHandler? OnReceiveFileTaskAdded;
        public event ReceiveFileTaskEventHandler? OnReceiveFileTaskRemoved;


        public List<ReceiveFileHandler> GetAllHandlers()
        {
            return [.. _caches.Select(s => s.Value)];
        }

        public void NotifyReceiveFileTaskAdded(ReceiveFileHandler handler)
        {
            OnReceiveFileTaskAdded?.Invoke(handler);
        }

        public void NotifyReceiveFileTaskRemoved(ReceiveFileHandler handler)
        {
            OnReceiveFileTaskRemoved?.Invoke(handler);
        }

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

        public void RemoveReceiveFileHandler(string taskId)
        {
            if (_caches.ContainsKey(taskId))
            {
                _caches.Remove(taskId);
            }
        }
    }
}
