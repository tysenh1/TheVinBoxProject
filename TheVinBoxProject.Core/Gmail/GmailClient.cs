using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace TheVinBoxProject.Core.Gmail
{
    public class GmailClient
    {
        private static readonly string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailSend };
        private const string ApplicationName = "TheVinBoxProject";
        private readonly GmailService _service;

        public GmailClient(UserCredential credential)
        {
            _service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public static async Task<UserCredential?> Authenticate()
        {
            string credentialsPath = "credentials.json";
            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore("TheVinBoxProject", true));
            }
        }

        public async Task<List<Email>> GetGmailMessages()
        {
            try
            {
                var listRequest = _service.Users.Messages.List("me");
                Console.WriteLine("How many emails would you like to summarize?");
                string maxResultsString = Console.ReadLine();
                listRequest.MaxResults = Convert.ToInt32(maxResultsString);
                listRequest.LabelIds = "INBOX";
                var messages = listRequest.Execute().Messages;

                var emails = new List<Email>();
                if (messages == null || messages.Count == 0)
                {
                    Console.WriteLine("No messages found.");
                    return emails;
                }

                foreach (var message in messages)
                {
                    var getRequest = _service.Users.Messages.Get("me", message.Id);
                    getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                    Message fullEmail = await getRequest.ExecuteAsync();

                    var emailObj = new Email();
                    if (fullEmail.Payload != null)
                    {
                        foreach (var header in fullEmail.Payload.Headers)
                        {
                            emailObj.Subject = (header.Name == "Subject" && header.Value != "") ? header.Value : "Empty";
                            emailObj.From = (header.Name == "From" && header.Value != "") ? header.Value : "Empty";
                        }

                        emailObj.Body = GetEmailBody(fullEmail.Payload);
                    }
                    emails.Add(emailObj);
                }
                return emails;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new List<Email>();
            }
        }

        private static string GetEmailBody(MessagePart payload)
        {
            string body = string.Empty;

            if (payload.Parts != null && payload.Parts.Count > 0)
            {
                var part = payload.Parts.FirstOrDefault(p => p.MimeType == "text/html" || p.MimeType == "text/plain");
                if (part?.Body?.Data != null)
                {
                    body = DecodeBase64UrlSafe(part.Body.Data);
                }
            }
            else if (payload.Body?.Data != null)
            {
                body = DecodeBase64UrlSafe(payload.Body.Data);
            }

            // Remove HTML tags using regex before returning the body.
            // This is the regex you requested.
            return Regex.Replace(body, "<[^>]+>", string.Empty);
        }

        private static string DecodeBase64UrlSafe(string base64UrlSafe)
        {
            string base64 = base64UrlSafe.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            byte[] data = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(data);
        }
    }

    public class Email
    {
        public string? From { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}