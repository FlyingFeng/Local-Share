using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTool
{
    public static class FileSizeFormatter
    {
        private const long KB = 1024;
        private const long MB = 1024 * KB;
        private const long GB = 1024 * MB;
        private const long TB = 1024 * GB;

        /// <summary>
        /// 格式化文件大小，自动选择合适的单位
        /// </summary>
        public static string Format(long bytes, int decimals = 2)
        {
            return bytes switch
            {
                < 0 => throw new ArgumentOutOfRangeException(nameof(bytes), "字节数不能为负数"),
                < KB => $"{bytes} B",
                < MB => $"{(double)bytes / KB}:F{decimals} KB",
                < GB => $"{(double)bytes / MB}:F{decimals} MB",
                < TB => $"{(double)bytes / GB}:F{decimals} GB",
                _ => $"{(double)bytes / TB}:F{decimals} TB"
            };
        }
    }
}
