using System;
using System.IO;
using System.Security.Cryptography;

namespace GamelistManager
{
    internal static class ChecksumCreator
    {
        public static string CreateMD5(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }
    }
}
