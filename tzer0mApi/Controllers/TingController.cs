using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using tzer0mApi.Services.FCM;

namespace tzer0mApi.Controllers
{
    /// <summary>
    /// Ting controller
    /// </summary>
    /// <param name="configuration">Configuration</param>
    [ApiController]
    [Route("[controller]")]
    public class TingController(IConfiguration configuration) : ControllerBase
    {
        // Constants for file paths
        private const string SERVICE_ACCOUNT_CREDENTIALS = "serviceAccountCredentials.json";
        private const string FCM_TOKEN_FILE = "Services/FCM/fcmToken.txt";

        /// <summary>
        /// RSS API key
        /// </summary>
        private string? RssApiKey { get; set; } = configuration["RSS_API_KEY"];

        /// <summary>
        /// Send notification to device
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="body">Notification body</param>
        /// <returns>Message content</returns>
        [HttpGet(Name = "Ting")]
        public async Task<string> Index(string title, string body)
        {
            // Get access and fcm tokens
            string accessToken = await FCMHelper.GetAccessToken(SERVICE_ACCOUNT_CREDENTIALS);
            string fcmToken = System.IO.File.ReadAllText(FCM_TOKEN_FILE);

            // Create client and request message
            HttpClient client = new();
            HttpRequestMessage requestMessage = new(HttpMethod.Post, "https://fcm.googleapis.com/v1/projects/ting-tzer0m/messages:send");

            // Add headers and content
            requestMessage.Headers.Add("Authorization", $"Bearer {accessToken}");
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(new FCMBody(fcmToken, title, body)));

            // Send request and return response
            HttpResponseMessage response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            // Post to RSS feed
            HttpRequestMessage rssRequest = new(HttpMethod.Post, "https://rss.tzer0m.co.uk/post");
            rssRequest.Headers.Add("X-API-Key", RssApiKey);
            rssRequest.Content = new StringContent(
                JsonConvert.SerializeObject(new { title, summary = "Ting", content = body }),
                Encoding.UTF8,
                "application/json"
            );
            await client.SendAsync(rssRequest);

            // Return response
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Updates FCM token
        /// </summary>
        /// <param name="body">Update body containing the new FCM token</param>
        /// <returns>Task</returns>
        [HttpPost("Update", Name = "Ting Update")]
        public void Update([FromBody] UpdateBody body)
        {
            if (!System.IO.File.Exists(FCM_TOKEN_FILE))
                System.IO.File.Create(FCM_TOKEN_FILE).Dispose();

            System.IO.File.WriteAllText(FCM_TOKEN_FILE, body.FCMToken);
        }
    }
}