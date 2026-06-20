using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Screentation;

public class ClipboardMonitor : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly SubclassProc _subclassProc;
    private readonly IntPtr _subclassId = new IntPtr(1001);
    private bool _isDisposed = false;
    private DateTime _lastCapturedTime = DateTime.MinValue;
    private int _lastWidth = 0;
    private int _lastHeight = 0;
    private readonly byte[] _lastSamples = new byte[40];

    private const uint WM_CLIPBOARDUPDATE = 0x031D;

    // Event invoked when a new image is captured
    public event EventHandler<SoftwareBitmap>? ImageCaptured;

    public ClipboardMonitor(Window window)
    {
        _hwnd = WindowNative.GetWindowHandle(window);
        _subclassProc = NewWindowProc;

        // Subclass window to hook message loop
        SetWindowSubclass(_hwnd, _subclassProc, _subclassId, IntPtr.Zero);
        
        // Register for clipboard updates
        AddClipboardFormatListener(_hwnd);
    }

    private IntPtr NewWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_CLIPBOARDUPDATE)
        {
            // Handle clipboard update asynchronously
            _ = ProcessClipboardAsync();
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    private const uint CF_DIB = 8;

    private (SoftwareBitmap? bitmap, bool isDuplicate) GetClipboardImageWin32()
    {
        if (!OpenClipboard(IntPtr.Zero)) return (null, false);
        try
        {
            IntPtr hDIB = GetClipboardData(CF_DIB);
            if (hDIB == IntPtr.Zero) return (null, false);

            IntPtr pDIB = GlobalLock(hDIB);
            if (pDIB == IntPtr.Zero) return (null, false);

            try
            {
                var header = Marshal.PtrToStructure<BITMAPINFOHEADER>(pDIB);
                
                int width = header.biWidth;
                int height = header.biHeight;
                ushort bitCount = header.biBitCount;

                if (bitCount != 32 && bitCount != 24) return (null, false);

                bool isBottomUp = height > 0;
                int absHeight = Math.Abs(height);

                // Read sample pixels from DIB for duplicate check
                byte[] samples = new byte[40];
                int pixelOffset = (int)header.biSize;
                if (header.biCompression == 3) // BI_BITFIELDS
                {
                    pixelOffset += 12;
                }

                IntPtr pixelStart = pDIB + pixelOffset;
                int srcStride = bitCount == 32 ? width * 4 : ((width * 24 + 31) / 32) * 4;

                int[] sampleCoords = new[]
                {
                    0, 0,
                    width / 2, 0,
                    width - 1, 0,
                    0, absHeight / 2,
                    width / 2, absHeight / 2,
                    width - 1, absHeight / 2,
                    0, absHeight - 1,
                    width / 2, absHeight - 1,
                    width - 1, absHeight - 1,
                    width / 3, absHeight / 3
                };

                for (int i = 0; i < 10; i++)
                {
                    int x = sampleCoords[i * 2];
                    int y = sampleCoords[i * 2 + 1];
                    int srcY = isBottomUp ? (absHeight - 1 - y) : y;
                    IntPtr pixelPtr = pixelStart + srcY * srcStride + x * (bitCount / 8);
                    
                    if (bitCount == 32)
                    {
                        samples[i * 4] = Marshal.ReadByte(pixelPtr);
                        samples[i * 4 + 1] = Marshal.ReadByte(pixelPtr + 1);
                        samples[i * 4 + 2] = Marshal.ReadByte(pixelPtr + 2);
                        samples[i * 4 + 3] = Marshal.ReadByte(pixelPtr + 3);
                    }
                    else // 24-bit
                    {
                        samples[i * 4] = Marshal.ReadByte(pixelPtr);
                        samples[i * 4 + 1] = Marshal.ReadByte(pixelPtr + 1);
                        samples[i * 4 + 2] = Marshal.ReadByte(pixelPtr + 2);
                        samples[i * 4 + 3] = 255;
                    }
                }

                // Check if identical to last captured
                bool isDuplicate = (width == _lastWidth && absHeight == _lastHeight);
                if (isDuplicate)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        if (samples[i] != _lastSamples[i])
                        {
                            isDuplicate = false;
                            break;
                        }
                    }
                }

                if (isDuplicate && (DateTime.UtcNow - _lastCapturedTime < TimeSpan.FromSeconds(2.0)))
                {
                    return (null, true);
                }

                // If not duplicate, allocate and copy pixel data
                int stride = width * 4;
                byte[] pixelData = new byte[stride * absHeight];

                if (bitCount == 32)
                {
                    for (int y = 0; y < absHeight; y++)
                    {
                        int srcY = isBottomUp ? (absHeight - 1 - y) : y;
                        IntPtr srcRow = pixelStart + srcY * width * 4;
                        int dstOffset = y * stride;
                        Marshal.Copy(srcRow, pixelData, dstOffset, stride);
                    }
                }
                else if (bitCount == 24)
                {
                    byte[] tempRow = new byte[srcStride];
                    for (int y = 0; y < absHeight; y++)
                    {
                        int srcY = isBottomUp ? (absHeight - 1 - y) : y;
                        IntPtr srcRow = pixelStart + srcY * srcStride;
                        Marshal.Copy(srcRow, tempRow, 0, srcStride);
                        
                        int dstOffset = y * stride;
                        for (int x = 0; x < width; x++)
                        {
                            pixelData[dstOffset + x * 4] = tempRow[x * 3]; // B
                            pixelData[dstOffset + x * 4 + 1] = tempRow[x * 3 + 1]; // G
                            pixelData[dstOffset + x * 4 + 2] = tempRow[x * 3 + 2]; // R
                            pixelData[dstOffset + x * 4 + 3] = 255; // A
                        }
                    }
                }

                // Update the duplicate-check cache
                _lastWidth = width;
                _lastHeight = absHeight;
                Array.Copy(samples, _lastSamples, 40);

                var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, absHeight, BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pixelData.AsBuffer());
                return (softwareBitmap, false);
            }
            finally
            {
                GlobalUnlock(hDIB);
            }
        }
        catch
        {
            return (null, false);
        }
        finally
        {
            CloseClipboard();
        }
    }

    private async Task ProcessClipboardAsync()
    {
        try
        {
            // First, quickly check if DIB format is available.
            // This is non-blocking and doesn't require opening the clipboard.
            if (!IsClipboardFormatAvailable(CF_DIB))
            {
                return;
            }

            // Small initial delay to let the writing application release its lock
            await Task.Delay(100);

            SoftwareBitmap? bitmap = null;
            bool isDuplicate = false;
            
            // Retry opening clipboard up to 10 times with 50ms delay
            for (int i = 0; i < 10; i++)
            {
                var result = GetClipboardImageWin32();
                if (result.isDuplicate)
                {
                    isDuplicate = true;
                    break;
                }
                bitmap = result.bitmap;
                if (bitmap != null) break;
                await Task.Delay(50);
            }

            if (isDuplicate)
            {
                return; // Exit immediately, it's a duplicate of the last captured screenshot
            }
            
            if (bitmap == null)
            {
                // Fallback to WinRT Clipboard if Win32 returns null (e.g. if focus is present)
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                {
                    var imageStreamRef = await dataPackageView.GetBitmapAsync();
                    using var stream = await imageStreamRef.OpenReadAsync();
                    
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    
                    if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                        softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                    {
                        var converted = SoftwareBitmap.Convert(
                            softwareBitmap, 
                            BitmapPixelFormat.Bgra8, 
                            BitmapAlphaMode.Premultiplied
                        );
                        softwareBitmap.Dispose();
                        softwareBitmap = converted;
                    }
                    bitmap = softwareBitmap;
                }
            }

            if (bitmap != null)
            {
                lock (this)
                {
                    if (DateTime.UtcNow - _lastCapturedTime < TimeSpan.FromMilliseconds(500))
                    {
                        bitmap.Dispose();
                        return;
                    }
                    _lastCapturedTime = DateTime.UtcNow;
                }

                ImageCaptured?.Invoke(this, bitmap);
            }
        }
        catch
        {
            // Ignore clipboard errors
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            RemoveClipboardFormatListener(_hwnd);
            RemoveWindowSubclass(_hwnd, _subclassProc, _subclassId);
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    // Win32 APIs
    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, IntPtr uIdSubclass);

    [DllImport("Comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);
}
