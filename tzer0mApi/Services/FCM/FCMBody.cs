using Newtonsoft.Json;

namespace tzer0mApi.Services.FCM
{
    /// <summary>
    /// Request body
    /// </summary>
    /// <param name="token">FCM token</param>
    /// <param name="title">Message title</param>
    /// <param name="body">Message body</param>
    public class FCMBody(string token, string title, string body)
    {
        /// <summary>
        /// Message
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public Message Message { get; set; } = new Message(token, title, body);
    }
}