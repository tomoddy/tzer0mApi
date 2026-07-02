using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using tzer0mApi.Services.OpenUV;
using tzer0mApi.Services.OpenUV.Objects;
using tzer0mApi.Services.Ting;

namespace tzer0mApi.Controllers
{
    /// <summary>
    /// Handles weather-related operations, including UV index retrieval and forecast data.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        /// <summary>
        /// Base url
        /// </summary>
        private string BaseUrl { get; set; }

        /// <summary>
        /// Api key
        /// </summary>
        private string ApiKey { get; set; }

        /// <summary>
        /// Ting service
        /// </summary>
        private TingService TingService { get; set; }

        /// <summary>
        /// Http client
        /// </summary>
        private HttpClient Client { get; set; } = new HttpClient();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="tingService">Ting service</param>
        /// <exception cref="NullReferenceException">Thrown if config values are missing</exception>
        public WeatherController(IConfiguration configuration, TingService tingService)
        {
            BaseUrl = configuration["Weather:OpenUV:BaseUrl"] ?? throw new NullReferenceException(nameof(BaseUrl));
            ApiKey = configuration["Weather:OpenUV:ApiKey"] ?? throw new NullReferenceException(nameof(ApiKey));
            TingService = tingService;
        }

        /// <summary>
        /// Gets UV report for given location and altitude. Altitude is optional, default is 0m. If altitude is not 0, the UV index will be higher than at sea level.
        /// </summary>
        /// <param name="latitude">Latitude</param>
        /// <param name="longitude">Longitude</param>
        /// <param name="altitude">Altitude (m)</param>
        /// <returns>UV report</returns>
        /// <exception cref="BadHttpRequestException">Data could not be fetched from OpenUV</exception>
        /// <exception cref="JsonException">Response could not be deserialised</exception>
        [HttpGet("UV", Name = "UV Report")]
        public async Task<UVReport> GetUVReport(double latitude, double longitude, int altitude = 0)
        {
            // Create url
            string url = QueryHelpers.AddQueryString(BaseUrl, new Dictionary<string, string?>
            {
                { "lat", latitude.ToString() },
                { "lng", longitude.ToString() },
                { "alt", altitude.ToString() }
            });

            // Create and send request, validate response
            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            requestMessage.Headers.Add("X-Access-Token", ApiKey);
            HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();

            // Deserialise response
            string? responseMessageContent = await responseMessage.Content.ReadAsStringAsync() ?? throw new BadHttpRequestException("Could not get data from OpenUV", 500);
            OpenUVResponse? openUVResponse = JsonConvert.DeserializeObject<OpenUVResponse>(responseMessageContent);

            // Check response, convert and return
            return openUVResponse is null ? throw new JsonException("Could not deserialise data from OpenUV") : new UVReport(openUVResponse);
        }

        /// <summary>
        /// Calls UV endpoint and send notification with details
        /// </summary>
        /// <param name="latitude">Latitude</param>
        /// <param name="longitude">Longitude</param>
        /// <param name="altitude">Altitude (m)</param>
        /// <returns>Task</returns>
        [HttpGet("UV/Notify", Name = "UV Report with Notification")]
        public async Task GetUVReportAndNotify(double latitude, double longitude, int altitude = 0)
        {
            UVReport report = await GetUVReport(latitude, longitude, altitude);
            UVPoint max = report.UVPoints.First(uVP => uVP.Window == "max");
            UVPoint morning = report.UVPoints.First(uVP => uVP.Window == "morning");
            UVPoint afternoon = report.UVPoints.First(uVP => uVP.Window == "afternoon");

            // Send notification and post to RSS feed
            string body = $"Morning: {morning.UVRounded} at {morning.Time:h:mm}\nMaximum: {max.UVRounded} at {max.Time:h:mm}\nAfternoon: {afternoon.UVRounded} at {afternoon.Time:h:mm}";
            await TingService.Send("UV Report", body, "UV Report");
        }
    }
}