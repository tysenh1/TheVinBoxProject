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
                
                Console.WriteLine("How many emails would you like to summarize?");
                string? maxResultsString = Console.ReadLine();
                Int32 maxResults = maxResultsString != "" ? Convert.ToInt32(maxResultsString) : 5;
                
                
                Console.WriteLine("Which label would you like to use emails from?");
                string? label = Console.ReadLine();

                if (String.IsNullOrEmpty(label))
                {
                    label = "INBOX";
                }
                
                List<Email> emails = await client.GetGmailMessages(maxResults, label);

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