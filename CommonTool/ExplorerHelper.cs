using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTool
{
    public static class ExplorerHelper
    {
        /// <summary>
        /// 打开文件夹
        /// </summary>
        public static void OpenFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"文件夹不存在: {folderPath}");

            Process.Start("explorer.exe", folderPath);
        }

        /// <summary>
        /// 打开文件夹并选中指定文件
        /// </summary>
        public static void OpenFolderAndSelectFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}");

            // /select 参数：打开资源管理器并高亮选中该文件
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }

        /// <summary>
        /// 自动判断路径类型，文件则选中，文件夹则直接打开
        /// </summary>
        public static void Open(string path)
        {
            if (File.Exists(path))
                OpenFolderAndSelectFile(path);
            else if (Directory.Exists(path))
                OpenFolder(path);
            else
                throw new IOException($"路径不存在: {path}");
        }
    }
}
