using Newtonsoft.Json;

namespace tzer0mApi.Services.Clockify
{
    /// <summary>
    /// Summary report request
    /// </summary>
    /// <param name="end">End date</param>
    /// <param name="start">Start date</param>
    public class SummaryReportRequest(DateTime end, DateTime start)
    {
        /// <summary>
        /// Date range end
        /// </summary>
        [JsonProperty("dateRangeEnd")]
        public DateTime DateRangeEnd { get; set; } = end;

        /// <summary>
        /// Date range start
        /// </summary>
        [JsonProperty("dateRangeStart")]
        public DateTime DateRangeStart { get; set; } = start;

        /// <summary>
        /// Summary filter
        /// </summary>
        [JsonProperty("summaryFilter")]
        public SummaryFilter SummaryFilter { get; set; } = new SummaryFilter();

        /// <summary>
        /// Rounded if true, not rounded if false
        /// </summary>
        [JsonProperty("rounding")]
        public bool Rounding { get; set; } = true;
    }
}