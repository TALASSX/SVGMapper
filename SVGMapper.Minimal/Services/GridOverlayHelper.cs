using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;

namespace SVGMapper.Minimal.Services
{
#nullable disable
    public static class GridOverlayHelper
    {
        public static void SetupImageAndGrid(
            Image imageControl,
            Canvas overlayCanvas,
            int gridPx,
            Brush? stroke = null,
            double strokeThickness = 1.0)
        {
            if (imageControl?.Source is not BitmapSource bmp) return;
            stroke ??= Brushes.Gray;

            int imgPxW = bmp.PixelWidth;
            int imgPxH = bmp.PixelHeight;

            var dpi = VisualTreeHelper.GetDpi(imageControl);
            double dpiScaleX = dpi.DpiScaleX;
            double dpiScaleY = dpi.DpiScaleY;

            double imageDipW = imgPxW / dpiScaleX;
            double imageDipH = imgPxH / dpiScaleY;

            overlayCanvas.Children.Clear();
            overlayCanvas.Width = imageDipW;
            overlayCanvas.Height = imageDipH;
            overlayCanvas.IsHitTestVisible = false;

            for (int x = 0; x <= imgPxW; x += gridPx)
            {
                double xd = (double)x / dpiScaleX;
                var line = new Line
                {
                    X1 = xd, Y1 = 0,
                    X2 = xd, Y2 = imageDipH,
                    Stroke = stroke,
                    StrokeThickness = strokeThickness,
                    SnapsToDevicePixels = true
                };
                overlayCanvas.Children.Add(line);
            }

            for (int y = 0; y <= imgPxH; y += gridPx)
            {
                double yd = (double)y / dpiScaleY;
                var line = new Line
                {
                    X1 = 0, Y1 = yd,
                    X2 = imageDipW, Y2 = yd,
                    Stroke = stroke,
                    StrokeThickness = strokeThickness,
                    SnapsToDevicePixels = true
                };
                overlayCanvas.Children.Add(line);
            }

            var transform = ImageCoordinateTransformer.CalculateTransform(
                new Size(imgPxW, imgPxH),
                new Size(imageControl.ActualWidth, imageControl.ActualHeight),
                dpiScaleX, dpiScaleY,
                ImageCoordinateTransformer.StretchMode.Uniform);

            overlayCanvas.RenderTransform = transform.ToViewTransform();

            void UpdateTransform(object s, SizeChangedEventArgs e)
            {
                var dpi2 = VisualTreeHelper.GetDpi(imageControl);
                var t = ImageCoordinateTransformer.CalculateTransform(
                    new Size(imgPxW, imgPxH),
                    new Size(imageControl.ActualWidth, imageControl.ActualHeight),
                    dpi2.DpiScaleX, dpi2.DpiScaleY,
                    ImageCoordinateTransformer.StretchMode.Uniform);
                overlayCanvas.RenderTransform = t.ToViewTransform();
            }

            imageControl.SizeChanged -= UpdateTransform;
            imageControl.SizeChanged += UpdateTransform;
        }
    }
}
