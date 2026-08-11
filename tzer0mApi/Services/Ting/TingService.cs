using Newtonsoft.Json;
using System.Text;
using tzer0mApi.Services.FCM;

namespace tzer0mApi.Services.Ting
{
    /// <summary>
    /// Ting service
    /// </summary>
    /// <param name="configuration">Configuration</param>
    public class TingService(IConfiguration configuration)
    {
        /// <summary>
        /// Service account credentials file path
        /// </summary>
        private const string SERVICE_ACCOUNT_CREDENTIALS = "serviceAccountCredentials.json";

        /// <summary>
        /// FCM token file path
        /// </summary>
        private const string FCM_TOKEN_FILE = "Services/FCM/fcmToken.txt";

        /// <summary>
        /// RSS API key
        /// </summary>
        private readonly string? RssApiKey = configuration["RssApiKey"];

        /// <summary>
        /// Send notification to device and post to RSS feed
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="body">Notification body</param>
        /// <param name="summary">RSS summary</param>
        /// <returns>Task</returns>
        public async Task Send(string title, string body, string summary = "Ting")
        {
            // Get access and fcm tokens
            string accessToken = await FCMHelper.GetAccessToken(SERVICE_ACCOUNT_CREDENTIALS);
            string fcmToken = File.ReadAllText(FCM_TOKEN_FILE);

            // Create client and request message
            HttpClient client = new();
            HttpRequestMessage requestMessage = new(HttpMethod.Post, "https://fcm.googleapis.com/v1/projects/ting-tzer0m/messages:send");

            // Add headers and content
            requestMessage.Headers.Add("Authorization", $"Bearer {accessToken}");
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(new FCMBody(fcmToken, title, body)));

            // Send request and check response
            HttpResponseMessage response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            // Post to RSS feed
            HttpRequestMessage rssRequest = new(HttpMethod.Post, "https://rss.tzer0m.co.uk/post");
            rssRequest.Headers.Add("X-API-Key", RssApiKey);
            rssRequest.Content = new StringContent(
                JsonConvert.SerializeObject(new { title, summary, content = body }),
                Encoding.UTF8,
                "application/json"
            );

            // Send request and check response
            HttpResponseMessage rssResponse = await client.SendAsync(rssRequest);
            rssResponse.EnsureSuccessStatusCode();
        }
    }
}