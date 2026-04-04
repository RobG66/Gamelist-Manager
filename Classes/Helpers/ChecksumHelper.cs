using System;
using System.IO;
using System.Security.Cryptography;

namespace Gamelist_Manager.Classes.Helpers
{
    internal static class ChecksumHelper
    {
        public static string CalculateMd5(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
                return Convert.ToHexString(hash);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static readonly uint[] CrcTable = CreateCrcTable();

        private static uint[] CreateCrcTable()
        {
            const uint poly = 0xEDB88320;
            var table = new uint[256];

            for (uint i = 0; i < table.Length; i++)
            {
                var crc = i;
                for (var j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ poly : crc >> 1;
                }
                table[i] = crc;
            }

            return table;
        }

        public static string CalculateCrc32(string filePath)
        {
            try
            {
                uint crc = 0xFFFFFFFF;

                using var stream = File.OpenRead(filePath);
                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (var i = 0; i < bytesRead; i++)
                    {
                        crc = (crc >> 1) ^ CrcTable[(crc ^ buffer[i]) & 0xFF];
                    }
                }

                crc ^= 0xFFFFFFFF;
                return crc.ToString("X8");
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
