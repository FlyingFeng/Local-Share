
namespace LocalShare.Desktop.Models
{
    public enum MessageType
    {
        Skip,
        ShowMask,
        CloseMask,
        RestartServer,
        RestartBrocast,
        RemoveCurrentNodeFinishedSendFileTask,
        RestartMulticast,
        ChatMessageArrived
    }


    public class MessageModel
    {
        public MessageType MessageType { get; set; }
        public object? Data { get; set; }
    }
}
