using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using tzer0mApi.Services.Clockify;

namespace tzer0mApi.Controllers
{
    /// <summary>
    /// Main controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HoursController : ControllerBase
    {
        /// <summary>
        /// Workspace id
        /// </summary>
        private string? WorkspaceId { get; set; }

        /// <summary>
        /// Api key
        /// </summary>
        private string? ApiKey { get; set; }

        /// <summary>
        /// Http client
        /// </summary>
        private HttpClient Client { get; set; }

        /// <summary>
        /// Http request message
        /// </summary>
        private HttpRequestMessage RequestMessage { get; set; }

        /// <summary>
        /// Start date
        /// </summary>
        private static DateTime Start => new(DateTime.Now.Year, DateTime.Now.Month, 1);

        /// <summary>
        /// End date
        /// </summary>
        private static DateTime End => new DateTime(DateTime.Now.Year, DateTime.Now.Month + 1, 1).AddMilliseconds(-1);

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public HoursController(IConfiguration configuration)
        {
            // Set properties
            WorkspaceId = configuration["Hours:WorkspaceId"];
            ApiKey = configuration["Hours:ApiKey"];

            // Create client and request
            Client = new HttpClient();
            RequestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://reports.api.clockify.me/v1/workspaces/{WorkspaceId}/reports/summary");
            RequestMessage.Headers.Add("X-Api-Key", ApiKey);
        }

        /// <summary>
        /// Index request
        /// </summary>
        /// <returns>Number of hours so far this month</returns>
        /// <exception cref="NullReferenceException">Time cannot be found</exception>
        [HttpGet(Name = "Hours")]
        public float? Index()
        {
            // Create and send request message
            RequestMessage.Content = new StringContent(JsonConvert.SerializeObject(new SummaryReportRequest(End, Start)), Encoding.UTF8, "application/json");
            HttpResponseMessage response = Client.SendAsync(RequestMessage).Result;
            response.EnsureSuccessStatusCode();

            // Parse and return result
            int? totalBillableTime = JsonConvert.DeserializeObject<SummaryReportResponse>(response.Content.ReadAsStringAsync().Result)?.Totals?.FirstOrDefault()?.TotalBillableTime;
            return totalBillableTime is null ? throw new NullReferenceException() : (float)totalBillableTime.Value / 3600;
        }
    }
}