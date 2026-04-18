using Newtonsoft.Json;

namespace tzer0mApi.Services.FCM
{
    /// <summary>
    /// Message
    /// </summary>
    /// <param name="token">FCM token</param>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body</param>
    public class Message(string token, string title, string body)
    {
        /// <summary>
        /// Notification
        /// </summary>
        [JsonProperty(PropertyName = "notification")]
        public Notification Notification { get; set; } = new Notification(title, body);

        /// <summary>
        /// FCM token
        /// </summary>
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; } = token;
    }
}