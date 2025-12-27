using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SVGMapper.Views
{
    public partial class FloorPlanView
    {
        private string? _backgroundSvg;
        private UIElement? _backgroundElement;
        private string? _backgroundImagePath;

        private void ClearBackground()
        {
            if (_backgroundElement != null)
            {
                ZoomCanvas.Canvas.Children.Remove(_backgroundElement);
                _backgroundElement = null;
            }
            _backgroundSvg = null;
            _backgroundImagePath = null;
        }

        public void LoadBackgroundSvg(string svgContent, string fileName)
        {
            ClearBackground();

            _backgroundSvg = svgContent;

            // Try to convert SVG to a DrawingGroup (requires SharpVectors). If available, render as a DrawingImage.
            var drawing = Services.SvgImportService.ConvertSvgToDrawing(svgContent);
            if (drawing != null)
            {
                var image = new System.Windows.Controls.Image
                {
                    Source = new DrawingImage(drawing),
                    Opacity = 0.95,
                    Stretch = Stretch.None,
                    IsHitTestVisible = false
                };

                // Place the image at the bottom of the canvas and keep a reference
                ZoomCanvas.Canvas.Children.Insert(0, image);
                Canvas.SetLeft(image, 0);
                Canvas.SetTop(image, 0);
                _backgroundElement = image;
                return;
            }

            // Fallback placeholder visual: show a semi-transparent label indicating a background was loaded.
            var label = new TextBlock
            {
                Text = "Background: " + fileName,
                Opacity = 0.6,
                FontSize = 12,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(label, 8);
            Canvas.SetTop(label, 8);

            ZoomCanvas.Canvas.Children.Insert(0, label);
            _backgroundElement = label;
        }

        public void LoadBackgroundImage(string path)
        {
            ClearBackground();

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new System.Uri(path);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();

            var image = new System.Windows.Controls.Image
            {
                Source = bmp,
                Opacity = 1.0,
                Stretch = Stretch.None,
                IsHitTestVisible = false
            };

            ZoomCanvas.Canvas.Children.Insert(0, image);
            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);
            _backgroundElement = image;
            _backgroundImagePath = path;
        }

        public string? GetBackgroundSvg() => _backgroundSvg;
        public string? GetBackgroundImagePath() => _backgroundImagePath;
    }
}