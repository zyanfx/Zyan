using System.Security.Principal;
using Zyan.Communication.Security;

namespace Zyan.Examples.MiniChat.Server
{
    public class NicknameAuthProvider : IAuthenticationProvider
    {
        public AuthResponseMessage Authenticate(AuthRequestMessage authRequest)
        {
            if (!authRequest.Credentials.ContainsKey("nickname"))
                return new AuthResponseMessage() 
                {
                    ErrorMessage="No valid credentials provided.",
                    Success=false
                };

            string nickname=(string)authRequest.Credentials["nickname"];

            if (string.IsNullOrEmpty(nickname))
                return new AuthResponseMessage() 
                {
                    ErrorMessage="No nickname specified.",
                    Success=false
                };
            
            if (Program.ActiveNicknames.Contains(nickname))
                return new AuthResponseMessage()
                {
                    ErrorMessage = string.Format("Nickname '{0}' is already in use.",nickname),
                    Success = false
                };

            return new AuthResponseMessage()
            {
                AuthenticatedIdentity = new GenericIdentity(nickname),
                Success = true
            };
        }
    }
}
