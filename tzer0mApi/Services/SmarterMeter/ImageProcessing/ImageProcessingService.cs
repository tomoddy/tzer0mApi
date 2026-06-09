using OpenCvSharp;

namespace tzer0mApi.Services.SmarterMeter.ImageProcessing;

/// <summary>
/// Handles image preprocessing with OpenCV ready for OCR.
/// </summary>
/// <remarks>
/// Initialises the service
/// </remarks>
public class ImageProcessingService(ILogger<ImageProcessingService> logger, IConfiguration configuration)
{
    /// <summary>
    /// Logger
    /// </summary>
    private readonly ILogger<ImageProcessingService> Logger = logger;

    /// <summary>
    /// Config
    /// </summary>
    private readonly IConfiguration Configuration = configuration;

    /// <summary>
    /// Processes raw image bytes through the OpenCV pipeline and returns the preprocessed image as a PNG byte array.
    /// </summary>
    /// <param name="imageBytes">The raw bytes of the uploaded image.</param>
    /// <returns>The preprocessed image as a PNG byte array, or null if processing failed.</returns>
    public byte[]? PreprocessImage(byte[] imageBytes)
    {
        try
        {
            // Load image from bytes into OpenCV Mat, returning null if decoding fails
            using Mat src = Mat.FromImageData(imageBytes, ImreadModes.Color);
            if (src.Empty())
                return null;

            // Optional crop to meter display region
            using Mat cropped = ApplyCrop(src);

            // Convert to greyscale
            using Mat grey = new();
            Cv2.CvtColor(cropped, grey, ColorConversionCodes.BGR2GRAY);

            // Sharpen the image
            using Mat sharpened = new();
            float[] kernelData = [0, -1, 0, -1, 5, -1, 0, -1, 0];
            using Mat kernel = new(3, 3, MatType.CV_32F);
            kernel.SetArray(kernelData);
            Cv2.Filter2D(grey, sharpened, -1, kernel);

            // Otsu threshold
            using Mat thresh = new();
            Cv2.Threshold(sharpened, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // Close small gaps in digit segments
            using Mat morphKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(7, 7));
            using Mat closed = new();
            Cv2.MorphologyEx(thresh, closed, MorphTypes.Close, morphKernel);

            // Additional dilation to fatten strokes
            using Mat dilateKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            using Mat dilated = new();
            Cv2.Dilate(closed, dilated, dilateKernel);

            // Scale up for better OCR accuracy
            using Mat scaled = new();
            Cv2.Resize(dilated, scaled, new Size(dilated.Width * 3, dilated.Height * 3), interpolation: InterpolationFlags.Cubic);

            // Add white border
            using Mat padded = new();
            Cv2.CopyMakeBorder(scaled, padded, 20, 20, 20, 20, BorderTypes.Constant, Scalar.White);

            // Invert to black text on white background
            using Mat inverted = new();
            Cv2.BitwiseNot(padded, inverted);
            return inverted.ToBytes(".png");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Image preprocessing failed");
            return null;
        }
    }

    /// <summary>
    /// Applies a crop to the image if enabled in configuration.
    /// </summary>
    /// <param name="src">The source image.</param>
    /// <returns>The cropped image, or a clone of the original if cropping is disabled.</returns>
    private Mat ApplyCrop(Mat src)
    {
        // Check if cropping is enabled in configuration
        bool enabled = Configuration.GetValue<bool>("SmarterMeter:Crop:Enabled");
        if (!enabled)
            return src.Clone();

        // Get cropping parameters from configuration
        int x = Configuration.GetValue<int>("SmarterMeter:Crop:X");
        int y = Configuration.GetValue<int>("SmarterMeter:Crop:Y");
        int w = Configuration.GetValue<int>("SmarterMeter:Crop:Width");
        int h = Configuration.GetValue<int>("SmarterMeter:Crop:Height");

        // Clamp to image bounds to avoid out-of-range errors
        Rect roi = new(Math.Max(0, x), Math.Max(0, y), Math.Min(w, src.Width - x), Math.Min(h, src.Height - y));
        return new Mat(src, roi);
    }
}