using TheVinBoxProject.Core.Gmail;
using TheVinBoxProject.Core.Gemini;
using System;
using System.IO;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using GenerativeAI;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;

namespace TheVinBoxProject.ConsoleApp
{
    class Program
    {

        static async Task Main(string[] args)
        {

            try
            {
                UserCredential? credential = await GmailClient.Authenticate();

                if (credential == null)
                {
                    throw new Exception("Error finding credentials");
                }

                GmailClient client = new GmailClient(credential);

                List<Email> emails = await client.GetGmailMessages();

                GeminiClient aiClient = new GeminiClient();

                string aiResp = await aiClient.SummarizeEmails(emails);

                Console.WriteLine(aiResp);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

        }
    }
}