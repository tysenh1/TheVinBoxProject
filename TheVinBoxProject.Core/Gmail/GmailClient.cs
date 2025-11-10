using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
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
        private static readonly string[] Scopes = { GmailService.Scope.GmailReadonly, GmailService.Scope.GmailSend, GmailService.Scope.GmailModify };
        private const string ApplicationName = "TheVinBoxProject";
        private readonly GmailService _service;
        private const string HistoryIdFile = "historyId.txt";

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

        public async Task<List<Email>> GetGmailMessages(Int32 amount, string label)
        {
            var emails = new List<Email>();
            var listRequest = _service.Users.Messages.List("me");
            listRequest.MaxResults = amount;
            listRequest.LabelIds = label;
            var messages = listRequest.Execute().Messages;

            if (messages == null || messages.Count == 0)
            {
                throw new Exception($"Error fetching emails from label: {label}");
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

        public async Task StartGmailWatch(string projectId, string pubSubTopic, string[] labelIds)
        {
            var watchRequest = new WatchRequest
            {
                TopicName = $"projects/{projectId}/topics/{pubSubTopic}",
                LabelIds = labelIds,
                LabelFilterBehavior = "INCLUDE"
            };
            
            var watchResponse = await _service.Users.Watch(watchRequest, "me").ExecuteAsync();
            Console.WriteLine($"Watch registered. Watch expires at: {watchResponse.Expiration}");
            
            
        }

        public async Task SendEmail(string recipient, string subject, string body)
        {
            var email = new MailMessage();
            email.To.Add(recipient);
            email.Subject = subject;
            email.Body = body;
            var mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(email);

            using (var ms = new MemoryStream())
            {
                mimeMessage.WriteTo(ms);
                string encodedEmail = Convert.ToBase64String(ms.ToArray())
                    .Replace('+', '-').Replace('/', '_').Replace("=", "");
                Message message = new Message
                {
                    Raw = encodedEmail
                };
                await _service.Users.Messages.Send(message, "me").ExecuteAsync();
            } 
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

        public async Task<ulong> SetHistoryId()
        {
            var profile = await _service.Users.GetProfile("me").ExecuteAsync();
            ulong lastHistoryId = profile.HistoryId ?? 0UL;
            
            SaveHistoryId(lastHistoryId);
            Console.WriteLine($"Initialized historyId to current: {lastHistoryId}");
            return lastHistoryId;
        }
        private void SaveHistoryId(ulong historyId)
        {
            File.WriteAllText(HistoryIdFile, historyId.ToString());
        }
    }

    public class Email
    {
        public string? From { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
