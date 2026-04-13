using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop
{
    internal static class LocalSettingKey
    {
        /// <summary>
        /// 要去掉
        /// </summary>
        internal const string KeyServerPort = "server_port";
        internal const string KeyBrocastPort = "brocast_port";
        internal const string KeySendNodeMaxCount = "send_node_max_count";
        internal const string KeySameNodeMaxSendFileCount = "same_node_max_send_file_count";
        internal const string KeyDownloadPath = "download_path";
    }
}
