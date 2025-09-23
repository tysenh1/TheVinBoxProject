using Google.Apis.Auth.OAuth2;
using TheVinBoxProject.Core.Gmail;
using TheVinBoxProject.UI.Services;

namespace TheVinBoxProject.UI
{
    public partial class MainPage : ContentPage
    {
        private readonly GmailClientService _gmailClientService = new GmailClientService();

        string[] mailboxes =
        {
            "Inbox",
            "Sent",
            "Draft",
            "Spam",
            "Trash",
            "Starred",
            "Important",
            "Custom"
        };

        private Dictionary<string, string> formData = new Dictionary<string, string>();

        public MainPage()
        {
            InitializeComponent();

            MailboxPicker.ItemsSource = mailboxes;

        }

        private async void OnAuthenticateButtonClicked(object sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = "Authentication status: Authenticating...";

                GmailClient client = await _gmailClientService.GetGmailClient();

                if (client != null)
                {
                    statusLabel.Text = "Authentication status: Logged In";
                    AuthenticateButton.IsVisible = false;
                    //TestGrid.IsVisible = true;
                }
                else
                {
                    statusLabel.Text = "Authentication status: Failed";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Authentication status: Error - {ex.Message}";
            }
        }

    }
}
