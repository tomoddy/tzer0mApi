using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using tzer0mApi.Services.FCM;

namespace tzer0mApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TingController : ControllerBase
    {
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
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(new Body(fcmToken, title, body)));

            // Send request and return response
            HttpResponseMessage response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}