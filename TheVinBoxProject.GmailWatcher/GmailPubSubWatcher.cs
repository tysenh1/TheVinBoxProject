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

namespace TheVinBoxProject.GmailWatcher 
{
    public class GmailPubSubWatcher
    {
        private const string ApplicationName = "2025 Stupid Hackathon";
        private const string CredentialFile = "credentials.json";
        private const string HistoryIdFile = "historyId.txt";

        private GmailService _gmailService;
        private SubscriberClient _subscriber;

        private ulong _lastHistoryId = 0;

        // Use your project, topic, and subscription names here:
        private readonly string ProjectId = "wide-world-472203-d7";
        private readonly string PubSubTopic = "gmail-notifications";
        private readonly string PubSubSubscription = "gmail-notification-sub";

        public async Task InitAsync()
        {
            // Initialize Gmail Service with OAuth2 credentials
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(new FileStream("credentials.json", FileMode.Open, FileAccess.Read)).Secrets,
                new[] { GmailService.Scope.GmailModify, GmailService.Scope.GmailReadonly },
                "user",
                CancellationToken.None,
                new FileDataStore("TokenStore", true)
            );


            _gmailService = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Load last saved historyId or get current one if none
            _lastHistoryId = LoadHistoryId();
            if (_lastHistoryId == 0)
            {
                var profile = await _gmailService.Users.GetProfile("me").ExecuteAsync();
                _lastHistoryId = profile.HistoryId ?? 0UL;  // Use null-coalescing to handle nulls safely
                SaveHistoryId(_lastHistoryId);
                Console.WriteLine($"Initialized historyId to current: {_lastHistoryId}");
            }

            else
            {
                Console.WriteLine($"Loaded saved historyId: {_lastHistoryId}");
            }

            // Start watch on Inbox label only
            var watchRequest = new WatchRequest
            {
                TopicName = $"projects/{ProjectId}/topics/{PubSubTopic}",
                LabelIds = new[] { "INBOX" },
                LabelFilterBehavior = "INCLUDE"
            };

            var watchResponse = await _gmailService.Users.Watch(watchRequest, "me").ExecuteAsync();
            Console.WriteLine($"Watch registered. Watch expires at: {watchResponse.Expiration}");

            // Setup Pub/Sub subscriber to listen for notifications
            var subscriptionName = SubscriptionName.FromProjectSubscription(ProjectId, PubSubSubscription);
            _subscriber = await SubscriberClient.CreateAsync(subscriptionName);

            await _subscriber.StartAsync(async (msg, ct) =>
            {
                Console.WriteLine($"Received Pub/Sub message {msg.MessageId}");
                try
                {
                    await ProcessHistoryAsync();
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

        private async Task ProcessHistoryAsync()
        {
            List<string> newMessageIds = new List<string>();
            var request = _gmailService.Users.History.List("me");
            request.StartHistoryId = _lastHistoryId;
            bool morePages = true;

            while (morePages)
            {
                var response = await request.ExecuteAsync();
                if (response.History != null)
                {
                    foreach (var history in response.History)
                    {
                        if (history.MessagesAdded != null)
                        {
                            foreach (var message in history.MessagesAdded)
                            {
                                newMessageIds.Add(message.Message.Id);
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(response.NextPageToken))
                    morePages = false;
                else
                {
                    request.PageToken = response.NextPageToken;
                }

                // Update lastHistoryId with latest returned by API
                if (response.HistoryId.HasValue)
                {
                    _lastHistoryId = response.HistoryId.Value;
                    SaveHistoryId(_lastHistoryId);
                }

            }

            Console.WriteLine($"Found {newMessageIds.Count} new messages.");

            foreach (var id in newMessageIds)
            {
                var message = await _gmailService.Users.Messages.Get("me", id).ExecuteAsync();
                Console.WriteLine($"Processing message with ID: {message.Id}");
                // Insert your email handling logic here
            }
        }

        private ulong LoadHistoryId()
        {
            if (File.Exists(HistoryIdFile))
            {
                var text = File.ReadAllText(HistoryIdFile);
                if (ulong.TryParse(text, out ulong val))
                    return val;
            }
            return 0;
        }

        private void SaveHistoryId(ulong historyId)
        {
            File.WriteAllText(HistoryIdFile, historyId.ToString());
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
