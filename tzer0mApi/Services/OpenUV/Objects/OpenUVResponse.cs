using Newtonsoft.Json;

namespace tzer0mApi.Services.OpenUV.Objects
{
    /// <summary>
    /// Response from OpenUV
    /// </summary>
    public class OpenUVResponse
    {
        /// <summary>
        /// List of results
        /// </summary>
        [JsonProperty("result")]
        public List<OpenUVResult>? Result { get; set; }
    }
}