using System;
using System.IO;
using System.Text;
using System.Windows.Media;

namespace SVGMapper.Services
{
    public static class SvgImportService
    {
        // Simple placeholder: read svg file contents as string.
        public static string LoadSvgAsString(string path)
        {
            return File.ReadAllText(path);
        }

        // Convert SVG content into a WPF DrawingGroup using SharpVectors if available.
        // Requires NuGet package: SharpVectors.Wpf (https://www.nuget.org/packages/SharpVectors.Wpf)
        public static DrawingGroup? ConvertSvgToDrawing(string svgContent)
        {
            try
            {
                // Attempt to use SharpVectors.StreamSvgConverter to get a DrawingGroup
                var settingsType = Type.GetType("SharpVectors.Renderers.Wpf.WpfDrawingSettings, SharpVectors.Wpf");
                var converterType = Type.GetType("SharpVectors.Converters.StreamSvgConverter, SharpVectors.Wpf");

                if (settingsType == null || converterType == null)
                    return null; // SharpVectors not available

                var settings = Activator.CreateInstance(settingsType);
                var converter = Activator.CreateInstance(converterType, new object[] { settings });

                var loadMethod = converterType.GetMethod("LoadStream", new[] { typeof(Stream) });
                var drawingProperty = converterType.GetProperty("Drawing");

                if (loadMethod == null || drawingProperty == null) return null;

                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(svgContent)))
                {
                    loadMethod.Invoke(converter, new object[] { ms });
                    var drawing = drawingProperty.GetValue(converter) as DrawingGroup;
                    return drawing;
                }
            }
            catch
            {
                // On any error, return null and fallback to placeholder
                return null;
            }
        }
    }
}