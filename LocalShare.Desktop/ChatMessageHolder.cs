using System.Collections.Concurrent;

namespace LocalShare.Desktop
{
    internal static class ChatMessageHolder
    {

        private static ConcurrentDictionary<string, List<MessageInfo>> _chatData = new ConcurrentDictionary<string, List<MessageInfo>>();


        internal static void AddChatHistoryData(string nodeName, MessageInfo messageModel)
        {
            if (!_chatData.TryGetValue(nodeName, out var list))
            {
                list = new List<MessageInfo>();
                _chatData[nodeName] = list;
            }

            list.Add(messageModel);
        }

        internal static List<MessageInfo> GetChatHistoryData(string nodeName)
        {
            if (_chatData.TryGetValue(nodeName, out var list) && list != null)
            {
                var result = new List<MessageInfo>();
                result.AddRange(list);
                return result;
            }
            return [];
        }


    }
}
