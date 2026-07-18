using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.Graphics.Canvas;

namespace Screentation;

public static class ExportManager
{
    public static async Task ExportSessionAsync(
        ScreenshotSession session, 
        int index, 
        string targetFolder, 
        string format, 
        int quality, 
        string nameTemplate)
    {
        // 1. Determine file name
        string indexString = index.ToString("D2"); // Zero-padded sequence index
        string baseName = nameTemplate.Replace("[N]", indexString);
        string extension = format.ToLowerInvariant() switch
        {
            "jpeg" => "jpg",
            "webp" => "webp",
            _ => "png"
        };
        string fileName = $"{baseName}.{extension}";
        string filePath = Path.Combine(targetFolder, fileName);

        // 2. Ensure target directory exists
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        // 3. Render high-resolution canvas
        var device = CanvasDevice.GetSharedDevice();
        int width = session.OriginalBitmap.PixelWidth;
        int height = session.OriginalBitmap.PixelHeight;

        using var renderTarget = new CanvasRenderTarget(device, width, height, 96);
        using (var ds = renderTarget.CreateDrawingSession())
        {
            // Clear or draw background
            ds.Clear(Microsoft.UI.Colors.Transparent);
            
            using var background = CanvasBitmap.CreateFromSoftwareBitmap(device, session.OriginalBitmap);
            ds.DrawImage(background);

            // Draw all vector annotations
            foreach (var element in session.Annotations)
            {
                AnnotationDrawer.DrawElement(device, ds, element, background);
            }
        }

        // 4. Save to target format
        Guid encoderId = format.ToLowerInvariant() switch
        {
            "jpeg" => BitmapEncoder.JpegEncoderId,
            "webp" => new Guid("e094b660-ad50-440a-9e0c-4b137c60b177"), // WebP WIC Encoder
            _ => BitmapEncoder.PngEncoderId
        };

        try
        {
            // Extract a SoftwareBitmap from RenderTarget by saving to PNG stream first (highly reliable)
            using var pngStream = new InMemoryRandomAccessStream();
            await renderTarget.SaveAsync(pngStream, CanvasBitmapFileFormat.Png);
            pngStream.Seek(0);
            
            var decoder = await BitmapDecoder.CreateAsync(pngStream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            using var memoryStream = new InMemoryRandomAccessStream();
            BitmapEncoder encoder;
            
            if (encoderId == BitmapEncoder.JpegEncoderId)
            {
                var propertySet = new BitmapPropertySet();
                var qualityValue = new BitmapTypedValue(Math.Clamp(quality / 100f, 0.0f, 1.0f), Windows.Foundation.PropertyType.Single);
                propertySet.Add("ImageQuality", qualityValue);
                encoder = await BitmapEncoder.CreateAsync(encoderId, memoryStream, propertySet);
            }
            else
            {
                encoder = await BitmapEncoder.CreateAsync(encoderId, memoryStream);
            }

            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();

            memoryStream.Seek(0);

            // Write to disk in a non-conflicting way
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
            await memoryStream.AsStreamForRead().CopyToAsync(fileStream);
        }
        catch (Exception ex)
        {
            // WebP WIC codec might not be installed. Fall back to PNG if WebP fails.
            if (encoderId == new Guid("e094b660-ad50-440a-9e0c-4b137c60b177"))
            {
                string fallbackPath = Path.Combine(targetFolder, $"{baseName}.png");
                using var memoryStream = new InMemoryRandomAccessStream();
                await renderTarget.SaveAsync(memoryStream, CanvasBitmapFileFormat.Png);
                memoryStream.Seek(0);

                using var fileStream = new FileStream(fallbackPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
                await memoryStream.AsStreamForRead().CopyToAsync(fileStream);
            }
            else
            {
                throw new IOException($"Failed to save image to {filePath}. {ex.Message}", ex);
            }
        }
    }
}
