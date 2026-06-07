using Microsoft.AspNetCore.Mvc;
using tzer0mApi.Services.FCM;
using tzer0mApi.Services.Ting;

namespace tzer0mApi.Controllers
{
    /// <summary>
    /// Ting controller
    /// </summary>
    /// <param name="tingService">Ting service</param>
    [ApiController]
    [Route("[controller]")]
    public class TingController(TingService tingService) : ControllerBase
    {
        // Constants for file paths
        private const string FCM_TOKEN_FILE = "Services/FCM/fcmToken.txt";

        /// <summary>
        /// Send notification to device
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="body">Notification body</param>
        /// <returns>Task</returns>
        [HttpGet(Name = "Ting")]
        public async Task Index(string title, string body)
        {
            await tingService.Send(title, body);
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