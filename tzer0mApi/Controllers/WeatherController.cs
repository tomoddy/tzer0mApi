using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using tzer0mApi.Services.OpenUV;
using tzer0mApi.Services.OpenUV.Objects;

namespace tzer0mApi.Controllers
{
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
        /// Http client
        /// </summary>
        private HttpClient Client { get; set; } = new HttpClient();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <exception cref="NullReferenceException">Thrown if config values are missing</exception>
        public WeatherController(IConfiguration configuration)
        {
            BaseUrl = configuration["Weather:OpenUV:BaseUrl"] ?? throw new NullReferenceException(nameof(BaseUrl));
            ApiKey = configuration["Weather:OpenUV:ApiKey"] ?? throw new NullReferenceException(nameof(ApiKey));
        }

        /// <summary>
        /// Gets UV report for given location and altitude. Altitude is optional, default is 0m. If altitude is not 0, the UV index will be higher than at sea level.
        /// </summary>
        /// <param name="latitude">Latitude</param>
        /// <param name="longitude">Longitude</param>
        /// <param name="altitude">Altitude (m)</param>
        /// <returns></returns>
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
    }
}