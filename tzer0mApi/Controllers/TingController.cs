using Microsoft.AspNetCore.Mvc;
using tzer0mApi.Models.Ting.Kuma;
using tzer0mApi.Models.Ting.Semaphore;
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

        /// <summary>
        /// Receives Uptime Kuma's webhook notification payload and relays it as a Ting push.
        /// </summary>
        /// <param name="payload">The webhook payload sent by Kuma on a monitor status change.</param>
        /// <returns>Task</returns>
        [HttpPost("Kuma", Name = "Ting Kuma")]
        public async Task Kuma([FromBody] KumaWebhookPayload payload)
        {
            string title = payload.Monitor?.Name ?? "Kuma";
            await tingService.Send(title, payload.Msg);
        }

        /// <summary>
        /// Receives Semaphore UI's webhook notification payload and relays it as a Ting push.
        /// </summary>
        /// <param name="payload">The Slack-formatted webhook payload sent by Semaphore on task completion.</param>
        /// <returns>Task</returns>
        [HttpPost("Semaphore", Name = "Ting Semaphore")]
        public async Task Semaphore([FromBody] SemaphoreWebhookPayload payload)
        {
            SemaphoreAttachment? attachment = payload.Attachments?.FirstOrDefault();
            string title = attachment?.Title ?? "Semaphore";
            string message = attachment?.Text ?? "no details provided";
            await tingService.Send(title, message);
        }
    }
}