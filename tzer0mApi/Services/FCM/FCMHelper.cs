using Google.Apis.Auth.OAuth2;

namespace tzer0mApi.Services.FCM
{
    /// <summary>
    /// Firebade cloud messaging
    /// </summary>
    public class FCMHelper
    {
        /// <summary>
        /// Get access token from service account json file
        /// </summary>
        /// <param name="filePath">Service account credentials file path</param>
        /// <returns>Access token</returns>
        public static async Task<string> GetAccessToken(string filePath)
        {
            GoogleCredential credential = CredentialFactory.FromFile<ServiceAccountCredential>(filePath).ToGoogleCredential().CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }
    }
}