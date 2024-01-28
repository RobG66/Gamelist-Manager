using System;
using System.IO;
using System.Security.Cryptography;
namespace GamelistManager
{
    public static class ChecksumCreator
    {
        public static string CreateMD5(string filePath)
        {
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
