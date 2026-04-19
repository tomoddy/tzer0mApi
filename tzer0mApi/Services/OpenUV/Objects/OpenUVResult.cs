using Newtonsoft.Json;

namespace tzer0mApi.Services.OpenUV.Objects
{
    /// <summary>
    /// OpenUV result
    /// </summary>
    public class OpenUVResult
    {
        /// <summary>
        /// UV index
        /// </summary>
        [JsonProperty("uv")]
        public float UV { get; set; }

        /// <summary>
        /// Time of measurement
        /// </summary>
        [JsonProperty("uv_time")]
        public DateTime UVTime { get; set; }
    }
}