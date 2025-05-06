using System.Runtime.InteropServices;

namespace GamelistManager.classes
{
    public static class CredentialManager
    {
        // Struct to represent a credential in memory
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public ulong LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        // Import the CredWrite function
        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

        // Import the CredRead function
        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

        // Import the CredDelete function
        [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, uint type, uint flags);

        // Import the CredFree function
        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr buffer);

        // Make methods public for external access
        public static bool SaveCredentials(string targetName, string userName, string userPassword)
        {
            try
            {
                var credential = new CREDENTIAL
                {
                    Flags = 0,
                    Type = 1, // CRED_TYPE_GENERIC
                    TargetName = targetName,
                    CredentialBlobSize = (uint)(userPassword.Length * 2),
                    CredentialBlob = Marshal.StringToCoTaskMemUni(userPassword),
                    Persist = 2, // CRED_PERSIST_LOCAL_MACHINE
                    UserName = userName
                };

                if (!CredWrite(ref credential, 0))
                {
                    throw new Exception("Failed to save credentials. Error: " + Marshal.GetLastWin32Error());
                }

                Marshal.FreeCoTaskMem(credential.CredentialBlob);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static (string UserName, string Password) GetCredentials(string targetName)
        {
            IntPtr credentialPtr;

            if (CredRead(targetName, 1, 0, out credentialPtr)) // CRED_TYPE_GENERIC
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);

                string userName = credential.UserName;
                string password = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);

                CredFree(credentialPtr);
                return (userName, password);
            }
            else
            {
                return (null!, null!);
            }
        }
    }
}
