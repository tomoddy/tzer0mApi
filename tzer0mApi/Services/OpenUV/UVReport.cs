using tzer0mApi.Services.OpenUV.Objects;

namespace tzer0mApi.Services.OpenUV
{
    /// <summary>
    /// UV report
    /// </summary>
    public class UVReport
    {
        /// <summary>
        /// List of UV data points
        /// </summary>
        public List<UVPoint> UVPoints { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="openUVResponse">OpenUV response</param>
        /// <exception cref="ArgumentException">Thrown is response contains no results</exception>
        public UVReport(OpenUVResponse openUVResponse)
        {
            // Check response
            if (openUVResponse.Result == null || openUVResponse.Result.Count == 0)
                throw new ArgumentException("OpenUV response does not contain any results");

            // Get max value and its index
            OpenUVResult maxValue = openUVResponse!.Result!.OrderByDescending(x => x.UV).First();
            int maxValueIndex = openUVResponse.Result.IndexOf(maxValue);

            // Create UV points for max, morning, and afternoon
            UVPoints =
            [
                new UVPoint("max", maxValue),
                new UVPoint("morning", openUVResponse.Result[(openUVResponse.Result.Count - maxValueIndex) / 2 + 1]),
                new UVPoint("afternoon", openUVResponse.Result[(openUVResponse.Result.Count - maxValueIndex) / 2 + maxValueIndex - 1]),
            ];
        }
    }
}