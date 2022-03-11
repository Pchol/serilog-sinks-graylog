using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Serilog.Sinks.Graylog.Core.Extensions
{
    public static class StringExtensions
    {
        public static byte[] Compress(this string source)
        {
            var resultStream = new MemoryStream();
            using (var gzipStream = new GZipStream(resultStream, CompressionMode.Compress))
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(source);
                gzipStream.Write(messageBytes, 0, messageBytes.Length);
            }
            return resultStream.ToArray();
        }

        /// <summary>
        /// Truncates the specified maximum length.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <param name="postfix"></param>
        /// <returns></returns>
        public static string Truncate(this string source, int maxLength, string postfix = "")
        {
            if (source.Length <= maxLength)
            {
                return source;
            }

            if (postfix.Length > 0)
            {
                var lengthWithoutPostfix = maxLength - postfix.Length;
                return source.Substring(0, lengthWithoutPostfix) + postfix;
            }
            else
            {
                return source.Substring(0, maxLength);
            }
        }

        public static string Expand(this string source)
        {
            return Environment.ExpandEnvironmentVariables(source);
        }
    }
}