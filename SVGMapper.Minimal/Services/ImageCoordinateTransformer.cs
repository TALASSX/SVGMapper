using System;
using System.Windows;
using System.Windows.Media;

namespace SVGMapper.Minimal.Services
{
    public static class ImageCoordinateTransformer
    {
        public enum StretchMode { Uniform, UniformToFill, Fill }

        public sealed class TransformResult
        {
            public double ScaleX { get; init; }
            public double ScaleY { get; init; }
            public double OffsetX { get; init; }
            public double OffsetY { get; init; }
            public Transform ToViewTransform()
            {
                var tg = new TransformGroup();
                tg.Children.Add(new ScaleTransform(ScaleX, ScaleY));
                tg.Children.Add(new TranslateTransform(OffsetX, OffsetY));
                return tg;
            }
        }

        public static TransformResult CalculateTransform(
            Size imagePxSize,
            Size controlSize,
            double dpiScaleX = 1.0,
            double dpiScaleY = 1.0,
            StretchMode stretch = StretchMode.Uniform)
        {
            if (imagePxSize.Width <= 0 || imagePxSize.Height <= 0)
                return new TransformResult { ScaleX = 1, ScaleY = 1, OffsetX = 0, OffsetY = 0 };

            var imageDipW = imagePxSize.Width / dpiScaleX;
            var imageDipH = imagePxSize.Height / dpiScaleY;

            double viewW = Math.Max(0.0, controlSize.Width);
            double viewH = Math.Max(0.0, controlSize.Height);
            if (viewW <= 0 || viewH <= 0)
                return new TransformResult { ScaleX = 1, ScaleY = 1, OffsetX = 0, OffsetY = 0 };

            double scaleX = viewW / imageDipW;
            double scaleY = viewH / imageDipH;
            double finalScaleX = 1.0, finalScaleY = 1.0;

            switch (stretch)
            {
                case StretchMode.Uniform:
                    var s = Math.Min(scaleX, scaleY);
                    finalScaleX = finalScaleY = s;
                    break;
                case StretchMode.UniformToFill:
                    var s2 = Math.Max(scaleX, scaleY);
                    finalScaleX = finalScaleY = s2;
                    break;
                case StretchMode.Fill:
                    finalScaleX = scaleX;
                    finalScaleY = scaleY;
                    break;
            }

            var renderedW = imageDipW * finalScaleX;
            var renderedH = imageDipH * finalScaleY;

            var offsetX = (viewW - renderedW) / 2.0;
            var offsetY = (viewH - renderedH) / 2.0;

            offsetX = Math.Round(offsetX, 4);
            offsetY = Math.Round(offsetY, 4);
            finalScaleX = Math.Round(finalScaleX, 8);
            finalScaleY = Math.Round(finalScaleY, 8);

            return new TransformResult
            {
                ScaleX = finalScaleX,
                ScaleY = finalScaleY,
                OffsetX = offsetX,
                OffsetY = offsetY
            };
        }

        public static Point TransformPoint(
            Point imagePixelPoint,
            Size imagePxSize,
            Size controlSize,
            double dpiScaleX = 1.0,
            double dpiScaleY = 1.0,
            StretchMode stretch = StretchMode.Uniform)
        {
            var t = CalculateTransform(imagePxSize, controlSize, dpiScaleX, dpiScaleY, stretch);
            var xDip = imagePixelPoint.X / dpiScaleX;
            var yDip = imagePixelPoint.Y / dpiScaleY;
            var xUi = xDip * t.ScaleX + t.OffsetX;
            var yUi = yDip * t.ScaleY + t.OffsetY;
            return new Point(Math.Round(xUi, 4), Math.Round(yUi, 4));
        }

        // Normalize a point from image pixel coordinates to 0..1 relative coordinates
        public static Point NormalizePoint(Point imagePixelPoint, Size imagePxSize)
        {
            if (imagePxSize.Width <= 0 || imagePxSize.Height <= 0)
                return new Point(0, 0);
            return new Point(imagePixelPoint.X / imagePxSize.Width, imagePixelPoint.Y / imagePxSize.Height);
        }

        // Denormalize a relative 0..1 point back to image pixel coordinates
        public static Point DenormalizeToPixel(Point normalizedPoint, Size imagePxSize)
        {
            return new Point(normalizedPoint.X * imagePxSize.Width, normalizedPoint.Y * imagePxSize.Height);
        }

        // Map a normalized 0..1 image-relative point to the control's UI coordinates (ActualWidth/ActualHeight)
        // taking Stretch=Uniform/UniformToFill/Fill and DPI into account.
        public static Point MapNormalizedToControl(
            Point normalizedPoint,
            Size imagePxSize,
            Size controlSize,
            double dpiScaleX = 1.0,
            double dpiScaleY = 1.0,
            StretchMode stretch = StretchMode.Uniform)
        {
            // Calculate transform that maps image DIPs -> UI
            var t = CalculateTransform(imagePxSize, controlSize, dpiScaleX, dpiScaleY, stretch);

            // image size in DIPs
            var imageDipW = imagePxSize.Width / dpiScaleX;
            var imageDipH = imagePxSize.Height / dpiScaleY;

            // normalized -> position within rendered image area in DIPs
            var xInImageDip = normalizedPoint.X * imageDipW;
            var yInImageDip = normalizedPoint.Y * imageDipH;

            var xUi = t.OffsetX + xInImageDip * t.ScaleX;
            var yUi = t.OffsetY + yInImageDip * t.ScaleY;

            return new Point(Math.Round(xUi, 4), Math.Round(yUi, 4));
        }
    }
}
