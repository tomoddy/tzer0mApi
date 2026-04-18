using Newtonsoft.Json;

namespace tzer0mApi.Services.FCM
{
    /// <summary>
    /// Update body
    /// </summary>
    /// <param name="fcmToken">FCM token</param>
    public class UpdateBody(string fcmToken)
    {
        /// <summary>
        /// FCM token
        /// </summary>
        [JsonProperty(PropertyName = "fcmToken")]
        public string FCMToken { get; set; } = fcmToken;
    }
}