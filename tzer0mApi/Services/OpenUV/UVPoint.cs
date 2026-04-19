using tzer0mApi.Services.OpenUV.Objects;

namespace tzer0mApi.Services.OpenUV
{
    /// <summary>
    /// UV data point
    /// </summary>
    /// <param name="window">Window of time label</param>
    /// <param name="result">Result</param>
    public class UVPoint(string window, OpenUVResult result)
    {
        /// <summary>
        /// Window of time
        /// </summary>
        public string Window { get; set; } = window;

        /// <summary>
        /// UV value
        /// </summary>
        public float UV { get; set; } = result.UV;

        /// <summary>
        /// Time of measurement
        /// </summary>
        public DateTime Time { get; set; } = result.UVTime;
    }
}