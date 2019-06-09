using App.Plugins;
using System.Data;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;
using System.Text;

namespace App.Identity
{
    public class LocalUserLoginProvider : ILoginProvider
    {
        public bool ValidateCredentials(string userName, string password, out ClaimsIdentity identity)
        {
            using (var pc = new PrincipalContext(ContextType.Machine))
            {
                bool isValid = false;
                DataSet data = DBConnection.GetQuery("select * from [ReportServer].[dbo].[Users] where login = '"+userName+"' and password = '"+ CreateMD5(password) + "'");
                if (data.Tables[0].Rows.Count > 0)
                {
                    isValid = true;
                }
                if (isValid)
                {
                    identity = new ClaimsIdentity(Startup.OAuthOptions.AuthenticationType);
                    identity.AddClaim(new Claim(ClaimTypes.Name, userName));
                }
                else
                {
                    identity = null;
                }

                return isValid;
            }
        }
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}