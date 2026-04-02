using Newtonsoft.Json;

namespace tzer0mApi.Services.Clockify
{
    /// <summary>
    /// Summary filter
    /// </summary>
    public class SummaryFilter()
    {
        /// <summary>
        /// Groups
        /// </summary>
        [JsonProperty("groups")]
        public List<string> Groups { get; set; } = ["PROJECT"];

        /// <summary>
        /// Sort solumn
        /// </summary>
        [JsonProperty("sortColumn")]
        public string SortColumn { get; set; } = "GROUP";

        /// <summary>
        /// Status
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; } = "ALL";
    }
}