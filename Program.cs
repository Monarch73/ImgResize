using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ImgResize <input directory> <output directory>");
    return;
}

string inputDir = Path.GetFullPath(args[0]);
string outputDir = Path.GetFullPath(args[1]);

if (!Directory.Exists(inputDir))
{
    Console.WriteLine($"Error: Input directory '{inputDir}' does not exist.");
    return;
}

Console.WriteLine($"Scanning '{inputDir}' for JPG images...");

var enumOptions = new EnumerationOptions
{
    IgnoreInaccessible = true,
    RecurseSubdirectories = true
};

var files = Directory.EnumerateFiles(inputDir, "*.*", enumOptions)
    .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

foreach (var file in files)
{
    ProcessImage(file, inputDir, outputDir);
}

Console.WriteLine("Done!");

static void ProcessImage(string inputFilePath, string baseInputDir, string baseOutputDir)
{
    try
    {
        // Calculate output path
        string relativePath = Path.GetRelativePath(baseInputDir, inputFilePath);
        string outputFilePath = Path.Combine(baseOutputDir, relativePath);

        // Create output directory if it doesn't exist
        string? outputDirectory = Path.GetDirectoryName(outputFilePath);
        if (outputDirectory != null && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        const long MaxBytes = (long)(1.4 * 1024 * 1024); // 1.4 MB
        if (new FileInfo(inputFilePath).Length < MaxBytes)
        {
            File.Copy(inputFilePath, outputFilePath, overwrite: true);
            Console.WriteLine($"Skipped: {relativePath} (Already under 1.4MB, copied directly)");
            return;
        }

        using var image = Image.Load(inputFilePath);

        // 4 Megapixels = 4,000,000 pixels
        const double MaxPixels = 4_000_000.0;
        double currentPixels = image.Width * image.Height;

        if (currentPixels > MaxPixels)
        {
            double ratio = Math.Sqrt(MaxPixels / currentPixels);
            int newWidth = (int)Math.Round(image.Width * ratio);
            int newHeight = (int)Math.Round(image.Height * ratio);
            
            image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
        }

        // Compress until <= 1.4 MB
        int minQuality = 1;
        int maxQuality = 100;
        int bestQuality = 100;
        byte[]? bestBytes = null;

        // Try max quality first
        bestBytes = TrySave(image, maxQuality);
        if (bestBytes.Length > MaxBytes)
        {
            // Binary search for highest quality that satisfies size constraint
            int low = minQuality;
            int high = maxQuality - 1;
            
            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                byte[] midBytes = TrySave(image, mid);
                
                if (midBytes.Length <= MaxBytes)
                {
                    bestQuality = mid;
                    bestBytes = midBytes;
                    low = mid + 1; // Try to get higher quality
                }
                else
                {
                    high = mid - 1; // Need lower size -> lower quality
                }
            }
            
            // If even quality 1 is larger than MaxBytes
            if (bestBytes == null || bestBytes.Length > MaxBytes)
            {
                bestQuality = minQuality;
                bestBytes = TrySave(image, bestQuality);
                Console.WriteLine($"Warning: {relativePath} could not be compressed under 1.4MB even at lowest quality.");
            }
        }

        File.WriteAllBytes(outputFilePath, bestBytes);
        Console.WriteLine($"Processed: {relativePath} -> Quality: {bestQuality}, Size: {bestBytes.Length / 1024.0 / 1024.0:F2} MB");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing image {inputFilePath}: {ex.Message}");
    }
}

static byte[] TrySave(Image image, int quality)
{
    using var ms = new MemoryStream();
    var encoder = new JpegEncoder
    {
        Quality = quality
    };
    image.SaveAsJpeg(ms, encoder);
    return ms.ToArray();
}
