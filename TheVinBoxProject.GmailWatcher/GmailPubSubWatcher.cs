using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Cloud.PubSub.V1;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Util.Store;
using TheVinBoxProject.Core.Gemini;
using TheVinBoxProject.Core.Gmail;
using TheVinBoxProject.Core.Prompts;
using DotNetEnv;

namespace TheVinBoxProject.GmailWatcher 
{
    public class GmailPubSubWatcher
    {
        private const string CredentialFile = "credentials.json";
        private const string HistoryIdFile = "historyId.txt";

        private GmailClient client;
        private SubscriberClient _subscriber;
        private GeminiClient aiClient = new GeminiClient();

        private ulong _lastHistoryId = 0;

        // Use your project, topic, and subscription names here:
        private readonly string ProjectId = "wide-world-472203-d7";
        private readonly string PubSubTopic = "gmail-notifications";
        private readonly string PubSubSubscription = "gmail-notification-sub";

        public async Task InitAsync()
        {
            Env.Load();
            string? emailAddress = Environment.GetEnvironmentVariable("EMAIL_ADDRESS");
            if (string.IsNullOrEmpty(emailAddress))
            {
                throw new ArgumentNullException(nameof(emailAddress), "EMAIL_ADDRESS environment variable is not set.");
            }
            UserCredential? credential = await GmailClient.Authenticate();

            if (credential == null)
            {
                throw new Exception("Error finding credentials");
            }

            client = new GmailClient(credential);


            _lastHistoryId = await client.SetHistoryId();

            
            await client.StartGmailWatch(ProjectId, PubSubTopic, new[] { "INBOX"});

            // Setup Pub/Sub subscriber to listen for notifications
            var subscriptionName = SubscriptionName.FromProjectSubscription(ProjectId, PubSubSubscription);
            _subscriber = await SubscriberClient.CreateAsync(subscriptionName);

            await _subscriber.StartAsync(async (msg, ct) =>
            {
                Console.WriteLine($"Received Pub/Sub message {msg.MessageId}");
                try
                {
                    // await ProcessHistoryAsync();
                    List<Email> email = await client.GetGmailMessages(1, "INBOX");
                    
                    string summary = await aiClient.SummarizeEmails(email, Prompts.DominicTorettoPrompt);
                    Console.WriteLine(summary);
                    await client.SendEmail(emailAddress, "Test", summary);
                    return SubscriberClient.Reply.Ack;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing messages: {ex.Message}");
                    return SubscriberClient.Reply.Nack;
                }
            });

            Console.WriteLine("Listening to Pub/Sub notifications...");
        }



        public async Task ShutdownAsync()
        {
            if (_subscriber != null)
            {
                await _subscriber.StopAsync(CancellationToken.None);
                Console.WriteLine("Stopped Pub/Sub subscriber.");
            }
        }
    }
}
