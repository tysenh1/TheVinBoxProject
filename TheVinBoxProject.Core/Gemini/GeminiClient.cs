using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using GenerativeAI;
using TheVinBoxProject.Core.Prompts;
using DotNetEnv;
using TheVinBoxProject.Core.Gmail;

namespace TheVinBoxProject.Core.Gemini
{
    public class GeminiClient
    {
        private readonly GenerativeModel _model;
        public GeminiClient()
        {
            Env.Load();

            string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "GEMINI_API_KEY environment variable is not set.");
            }
            var geminiService = new GenerativeAI.GoogleAi(apiKey);
            _model = geminiService.CreateGenerativeModel("models/gemini-2.5-flash");
        }

        public async Task<string> SummarizeEmails(List<Email> emails)
        {
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
                        prompt = Prompts.Prompts.DominicTorettoPrompt;
                    } else if (whichPromptNumber == 2)
                    {
                        prompt = Prompts.Prompts.DJKhaledPrompt;
                    } else if (whichPromptNumber == 3)
                    {
                        prompt = Prompts.Prompts.Fortnite;
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

            var allEmails = new StringBuilder();

            foreach (var email in emails)
            {
                allEmails.AppendLine($"From: {email.From}");
                allEmails.AppendLine($"Subject: {email.Subject}");
                allEmails.AppendLine(email.Body);
                allEmails.AppendLine("---");
            }

            var resp = await _model.GenerateContentAsync(prompt + allEmails);
            

            return resp.ToString();
        }
    }
}