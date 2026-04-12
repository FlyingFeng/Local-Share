using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CommonTool
{
    public static class FileHashHelper
    {
        /// <summary>
        /// 计算文件 MD5（同步）
        /// </summary>
        public static string ComputeMd5(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant(); // e.g. "d41d8cd98f00b204e9800998ecf8427e"
        }

        /// <summary>
        /// 计算文件 MD5（异步，适合大文件）
        /// </summary>
        public static async Task<string> ComputeMd5Async(
            string filePath, CancellationToken ct = default)
        {
            using var md5 = MD5.Create();
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,  // 80KB 缓冲区
                useAsync: true);

            var hash = await md5.ComputeHashAsync(stream, ct);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// 计算字节数组的 MD5
        /// </summary>
        public static string ComputeMd5(byte[] bytes)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// 校验文件 MD5 是否匹配
        /// </summary>
        public static async Task<bool> VerifyMd5Async(
            string filePath, string expectedMd5, CancellationToken ct = default)
        {
            var actual = await ComputeMd5Async(filePath, ct);
            return string.Equals(actual, expectedMd5, StringComparison.OrdinalIgnoreCase);
        }
    }
}
