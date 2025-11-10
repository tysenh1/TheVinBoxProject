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

        public async Task<string> SummarizeEmails(List<Email> emails, string prompt)
        {
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