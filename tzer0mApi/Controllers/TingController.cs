using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using tzer0mApi.Services.FCM;

namespace tzer0mApi.Controllers
{
    /// <summary>
    /// Ting controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class TingController : ControllerBase
    {
        // Constants for file paths
        private const string SERVICE_ACCOUNT_CREDENTIALS = "serviceAccountCredentials.json";
        private const string FCM_TOKEN_FILE = "Services/FCM/fcmToken.txt";

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