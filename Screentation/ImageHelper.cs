using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Screentation;

public static class ImageHelper
{
    public static async Task<SoftwareBitmap> ScaleSoftwareBitmapAsync(SoftwareBitmap source, int targetWidth)
    {
        double aspect = (double)source.PixelHeight / source.PixelWidth;
        int targetHeight = (int)Math.Max(1, targetWidth * aspect);

        using var stream = new InMemoryRandomAccessStream();
        
        // Encode original bitmap to PNG in memory
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetSoftwareBitmap(source);
        
        // Set target size
        encoder.BitmapTransform.ScaledWidth = (uint)targetWidth;
        encoder.BitmapTransform.ScaledHeight = (uint)targetHeight;
        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant; // High quality scale
        
        await encoder.FlushAsync();
        stream.Seek(0);
        
        // Decode to a new SoftwareBitmap
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var scaled = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied
        );

        return scaled;
    }

    public static async Task<SoftwareBitmapSource> CreateSourceFromSoftwareBitmapAsync(SoftwareBitmap bitmap)
    {
        var source = new SoftwareBitmapSource();
        await source.SetBitmapAsync(bitmap);
        return source;
    }
}
