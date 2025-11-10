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
using TheVinBoxProject.Core.Prompts;

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
                
                Console.WriteLine("Would you like to use the Dominic Toretto, DJ Khaled or Fortnite prompt? (1 for Dom, 2 for Khaled, 3 for Fortnite)");
                string? whichPrompt = Console.ReadLine();
                string prompt;
                if (whichPrompt != null)
                {
                    try
                    {
                        int whichPromptNumber = Convert.ToInt32(whichPrompt);
                        if (whichPromptNumber == 1)
                        {
                            prompt = Prompts.DominicTorettoPrompt;
                        } else if (whichPromptNumber == 2)
                        {
                            prompt = Prompts.DJKhaledPrompt;
                        } else if (whichPromptNumber == 3)
                        {
                            prompt = Prompts.Fortnite;
                        } else
                        {
                            throw new Exception("Can only enter 1 or 2 for the summarizing personality");
                        }
                    } catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                } else
                {
                    throw new Exception("Invalid prompt input.");
                }

                string aiResp = await aiClient.SummarizeEmails(emails, prompt);

                Console.WriteLine(aiResp);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

        }
    }
}