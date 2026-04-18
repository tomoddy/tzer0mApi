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
            string accessToken = await FCMHelper.GetAccessToken("serviceAccountCredentials.json");
            string fcmToken = System.IO.File.ReadAllText("Services/FCM/fcmToken.txt");

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
        public async Task Update([FromBody] UpdateBody body)
        {
            System.IO.File.WriteAllText("Services/FCM/fcmToken.txt", body.FCMToken);
        }
    }
}