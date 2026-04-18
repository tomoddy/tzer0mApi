using Newtonsoft.Json;

namespace tzer0mApi.Services.FCM
{
    /// <summary>
    /// Notification
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body</param>
    public class Notification(string title, string body)
    {
        /// <summary>
        /// Notification title
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; } = title;

        /// <summary>
        /// Notification body
        /// </summary>
        [JsonProperty(PropertyName = "body")]
        public string Body { get; set; } = body;
    }
}