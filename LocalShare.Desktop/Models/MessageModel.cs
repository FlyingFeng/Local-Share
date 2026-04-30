using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        RestartMulticast
    }


    public class MessageModel
    {
        public MessageType MessageType { get; set; }
        public object? Data { get; set; }
    }
}
