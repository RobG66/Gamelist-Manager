using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Gamelist_Manager.Services;

namespace Gamelist_Manager.Classes.Helpers;

public static class CredentialHelper
{
    private class CredentialData
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private const string CredentialsKey = "EncryptedCredentials";
    private const string CredentialsSection = "Credentials";

    private static string CredentialsFilePath =>
        Path.Combine(AppContext.BaseDirectory, "ini", "credentials.ini");

    public static bool SaveCredentials(string service, string? userName, string? userPassword)
    {
        try
        {
            // Load existing credentials
            var allCredentials = LoadAllCredentials();

            // Update or add
            allCredentials[service] = new CredentialData
            {
                UserName = userName ?? string.Empty,
                Password = userPassword ?? string.Empty
            };

            // Serialize and encrypt
            var json = JsonSerializer.Serialize(allCredentials);
            var encrypted = EncryptString(json);

            // Save directly to the global credentials file
            var allSections = IniFileService.ReadIniFile(CredentialsFilePath);
            if (!allSections.ContainsKey(CredentialsSection))
                allSections[CredentialsSection] = new Dictionary<string, string>();
            allSections[CredentialsSection][CredentialsKey] = encrypted;
            IniFileService.WriteIniFile(CredentialsFilePath, allSections);

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
            return (null!, null!);

        var allCredentials = LoadAllCredentials();

        return allCredentials?.TryGetValue(targetName, out var credential) == true
            ? (credential.UserName, credential.Password)
            : (null!, null!);
    }


    // Currently not used
    public static bool DeleteCredentials(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return false;

        try
        {
            var allCredentials = LoadAllCredentials();

            if (!allCredentials.Remove(targetName))
                return true; // Doesn't exist

            // Serialize and encrypt
            var jsonString = JsonSerializer.Serialize(allCredentials);
            var encryptString = EncryptString(jsonString);

            // Save directly to the global credentials file
            var allSections = IniFileService.ReadIniFile(CredentialsFilePath);
            if (!allSections.ContainsKey(CredentialsSection))
                allSections[CredentialsSection] = new Dictionary<string, string>();
            allSections[CredentialsSection][CredentialsKey] = encryptString;
            IniFileService.WriteIniFile(CredentialsFilePath, allSections);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, CredentialData> LoadAllCredentials()
    {
        try
        {
            var section = IniFileService.GetSection(CredentialsFilePath, CredentialsSection);
            var encrypted = section != null && section.TryGetValue(CredentialsKey, out var val) ? val : string.Empty;
            if (string.IsNullOrEmpty(encrypted))
                return new Dictionary<string, CredentialData>();

            var jsonString = DecryptString(encrypted);
            if (string.IsNullOrEmpty(jsonString))
                return new Dictionary<string, CredentialData>();

            var credentials = JsonSerializer.Deserialize<Dictionary<string, CredentialData>>(jsonString);
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
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Use DPAPI on Windows, fallback to base64 elsewhere
            var encryptedBytes =
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform
                    .Windows)
                    ? System.Security.Cryptography.ProtectedData.Protect(plainBytes, null,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser)
                    : plainBytes; // For non-Windows platforms, just use Base64 encoding (obfuscation only)

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
            var encryptedBytes = Convert.FromBase64String(encryptedText);

            var plainBytes =
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform
                    .Windows)
                    ? System.Security.Cryptography.ProtectedData.Unprotect(encryptedBytes, null,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser)
                    : encryptedBytes; // For non-Windows platforms

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}