using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheVinBoxProject.Core.Gmail;

namespace TheVinBoxProject.UI.Services
{
    public class GmailClientService
    {
        private GmailClient? _client;

        public async Task<GmailClient> GetGmailClient()
        {
            if (_client == null)
            {
                UserCredential? credential = await GmailClient.Authenticate();

                if (credential == null)
                {
                    throw new Exception("Error finding credentials");
                }

                _client = new GmailClient(credential);
            }
            return _client;
        }
    }


}
