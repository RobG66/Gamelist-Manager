using CredentialManagement;

namespace GamelistManager
{
    internal static class CredentialManager
    {
        public static (string, string) GetCredentials(string credentialName)
        {
            string userName = string.Empty;
            string userPassword = string.Empty;

            Credential cred = new Credential { Target = credentialName };
            if (cred.Load())
            {
                userName = cred.Username;
                userPassword = cred.Password;
            }
            return (userName, userPassword);
        }

        public static bool SaveCredentials(string credentialName, string userID, string userPassword)
        {
            try
            {
                Credential CredentialManager = new Credential()
                {
                    Target = credentialName,
                    Username = userID,
                    Password = userPassword,
                    PersistanceType = PersistanceType.LocalComputer, // Choose appropriate persistence type
                };
                CredentialManager.Save();
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
