using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GamelistManager.classes.helpers
{
    public static class CredentialHelper
    {
        // Class to store credential data
        private class CredentialData
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public static bool SaveCredentials(string targetName, string userName, string userPassword)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return false;
            }

            try
            {
                // Get existing credentials dictionary
                var allCredentials = GetAllCredentials();

                // Update or add the credential
                allCredentials[targetName] = new CredentialData
                {
                    UserName = userName ?? string.Empty,
                    Password = userPassword ?? string.Empty
                };

                // Serialize and encrypt
                string json = JsonSerializer.Serialize(allCredentials);
                string encrypted = EncryptString(json);

                // Save to settings
                Properties.Settings.Default.EncryptedCredentials = encrypted;
                Properties.Settings.Default.Save();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static (string UserName, string Password) GetCredentials(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return (null!, null!);
            }

            try
            {
                var allCredentials = GetAllCredentials();

                if (allCredentials.TryGetValue(targetName, out var credential))
                {
                    return (credential.UserName, credential.Password);
                }

                return (null!, null!);
            }
            catch
            {
                return (null!, null!);
            }
        }

        public static bool DeleteCredentials(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return false;
            }

            try
            {
                var allCredentials = GetAllCredentials();

                if (!allCredentials.ContainsKey(targetName))
                {
                    return true; // Already doesn't exist
                }

                allCredentials.Remove(targetName);

                // Serialize and encrypt
                string json = JsonSerializer.Serialize(allCredentials);
                string encrypted = EncryptString(json);

                // Save to settings
                Properties.Settings.Default.EncryptedCredentials = encrypted;
                Properties.Settings.Default.Save();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Dictionary<string, CredentialData> GetAllCredentials()
        {
            try
            {
                string? encrypted = Properties.Settings.Default.EncryptedCredentials;

                if (string.IsNullOrEmpty(encrypted))
                {
                    return new Dictionary<string, CredentialData>();
                }

                string json = DecryptString(encrypted);

                if (string.IsNullOrEmpty(json))
                {
                    return new Dictionary<string, CredentialData>();
                }

                var credentials = JsonSerializer.Deserialize<Dictionary<string, CredentialData>>(json);
                return credentials ?? new Dictionary<string, CredentialData>();
            }
            catch
            {
                return new Dictionary<string, CredentialData>();
            }
        }

        private static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes;

                // Use DPAPI on Windows, fallback to base64 elsewhere
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                }
                else
                {
                    // For non-Windows platforms, just use Base64 encoding
                    // Note: This is not secure encryption, just obfuscation
                    // For true cross-platform encryption, use a library like libsodium
                    encryptedBytes = plainBytes;
                }

                return Convert.ToBase64String(encryptedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] plainBytes;

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                }
                else
                {
                    // For non-Windows platforms
                    plainBytes = encryptedBytes;
                }

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}