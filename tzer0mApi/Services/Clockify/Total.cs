using Newtonsoft.Json;

namespace tzer0mApi.Services.Clockify
{
    /// <summary>
    /// Total
    /// </summary>
    public class Total
    {
        /// <summary>
        /// Total billable time
        /// </summary>
        [JsonProperty("totalBillableTime")]
        public int TotalBillableTime { get; set; }
    }
}