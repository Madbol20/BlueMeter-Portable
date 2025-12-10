using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace BlueMeter.WPF.Helpers;

/// <summary>
/// Helper class for creating custom cursors from images
/// </summary>
public static class CursorHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr CreateIconIndirect(ref IconInfo icon);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential)]
    private struct IconInfo
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    /// <summary>
    /// Create a cursor from a PNG image file
    /// </summary>
    /// <param name="imagePath">Path to the PNG image</param>
    /// <param name="hotspotX">X coordinate of the cursor hotspot (default: 0)</param>
    /// <param name="hotspotY">Y coordinate of the cursor hotspot (default: 0)</param>
    /// <param name="rotationAngle">Rotation angle in degrees (positive = clockwise, negative = counter-clockwise)</param>
    /// <returns>Custom cursor or null if creation failed</returns>
    public static Cursor? CreateCursorFromImage(string imagePath, int hotspotX = 0, int hotspotY = 0, float rotationAngle = 0)
    {
        try
        {
            if (!File.Exists(imagePath))
                return null;

            using var originalBitmap = new System.Drawing.Bitmap(imagePath);
            using var drawingBitmap = rotationAngle != 0
                ? RotateImage(originalBitmap, rotationAngle)
                : originalBitmap;

            var hIcon = drawingBitmap.GetHicon();

            var iconInfo = new IconInfo();
            GetIconInfo(hIcon, ref iconInfo);

            iconInfo.xHotspot = hotspotX;
            iconInfo.yHotspot = hotspotY;
            iconInfo.fIcon = false; // false = cursor, true = icon

            var cursorHandle = CreateIconIndirect(ref iconInfo);

            if (cursorHandle != IntPtr.Zero)
            {
                var cursor = CursorInteropHelper.Create(new SafeCursorHandle(cursorHandle));
                return cursor;
            }

            DestroyIcon(hIcon);
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Rotate an image by the specified angle
    /// </summary>
    private static System.Drawing.Bitmap RotateImage(System.Drawing.Bitmap bitmap, float angle)
    {
        // Calculate the size needed to fit the rotated image without clipping
        double angleRad = angle * Math.PI / 180.0;
        double cos = Math.Abs(Math.Cos(angleRad));
        double sin = Math.Abs(Math.Sin(angleRad));
        int newWidth = (int)(bitmap.Width * cos + bitmap.Height * sin);
        int newHeight = (int)(bitmap.Width * sin + bitmap.Height * cos);

        // Create a larger bitmap to accommodate rotation
        var rotatedBitmap = new System.Drawing.Bitmap(newWidth, newHeight);
        rotatedBitmap.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

        using (var graphics = System.Drawing.Graphics.FromImage(rotatedBitmap))
        {
            // Make background transparent
            graphics.Clear(System.Drawing.Color.Transparent);

            // Set high quality rendering
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            // Rotate around the center of the new bitmap
            graphics.TranslateTransform(newWidth / 2f, newHeight / 2f);
            graphics.RotateTransform(angle);
            graphics.TranslateTransform(-bitmap.Width / 2f, -bitmap.Height / 2f);

            // Draw the rotated image
            graphics.DrawImage(bitmap, new System.Drawing.Point(0, 0));
        }

        return rotatedBitmap;
    }

    /// <summary>
    /// Safe handle wrapper for cursor
    /// </summary>
    private class SafeCursorHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeCursorHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return DestroyIcon(handle);
        }
    }
}
