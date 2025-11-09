using System;
using System.Threading.Tasks;

namespace TheVinBoxProject.GmailWatcher 
{
  class Program
  {
    static async Task Main(string[] args)
    {
      Console.WriteLine("Gmail Pub/Sub Watcher starting...");

      var gmailWatcher = new GmailPubSubWatcher();

      // Initialize OAuth, service clients
      await gmailWatcher.InitAsync();

      Console.WriteLine("Watcher is active. Listening for new emails...");

      Console.WriteLine("Press ENTER to exit program.");

      // Wait for user shutdown
      Console.ReadLine();

      // Gracefully shutdown Pub/Sub subscriber
      await gmailWatcher.ShutdownAsync();

      Console.WriteLine("Watcher stopped. Exiting.");
    }
  }
}

