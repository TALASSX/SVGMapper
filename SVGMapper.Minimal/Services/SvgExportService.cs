using System.IO;
using System.Text;
using SVGMapper.Minimal.Models;

namespace SVGMapper.Minimal.Services
{
    public class SvgExportService
    {
        public string ExportToSvg(ProjectDocument doc)
        {
            var sb = new StringBuilder();
            // set svg size/viewBox to match background image when available
            if (doc.BackgroundImageWidth > 0 && doc.BackgroundImageHeight > 0)
            {
                sb.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{doc.BackgroundImageWidth}' height='{doc.BackgroundImageHeight}' viewBox='0 0 {doc.BackgroundImageWidth} {doc.BackgroundImageHeight}'>");
            }
            else
            {
                sb.AppendLine("<svg xmlns='http://www.w3.org/2000/svg'>");
            }

            // embed background image if present (include width/height)
            if (!string.IsNullOrWhiteSpace(doc.BackgroundImagePath) && File.Exists(doc.BackgroundImagePath))
            {
                try
                {
                    var bytes = File.ReadAllBytes(doc.BackgroundImagePath);
                    var ext = Path.GetExtension(doc.BackgroundImagePath).ToLowerInvariant();
                    var mime = ext switch
                    {
                        ".png" => "image/png",
                        ".jpg" => "image/jpeg",
                        ".jpeg" => "image/jpeg",
                        ".gif" => "image/gif",
                        ".bmp" => "image/bmp",
                        _ => "application/octet-stream"
                    };
                    var base64 = Convert.ToBase64String(bytes);
                    var w = doc.BackgroundImageWidth > 0 ? doc.BackgroundImageWidth.ToString() : "";
                    var h = doc.BackgroundImageHeight > 0 ? doc.BackgroundImageHeight.ToString() : "";
                    var wh = (w != "" && h != "") ? $" width='{w}' height='{h}'" : "";
                    sb.AppendLine($"<image href=\"data:{mime};base64,{base64}\" x=\"0\" y=\"0\"{wh} preserveAspectRatio=\"none\" />");
                }
                catch
                {
                    // ignore embed errors
                }
            }

            // polygons: model points are in DIPs (device independent units). Convert to image pixels using background DPI scale when available.
            var scaleX = doc.BackgroundDpiScaleX > 0 ? doc.BackgroundDpiScaleX : 1.0;
            var scaleY = doc.BackgroundDpiScaleY > 0 ? doc.BackgroundDpiScaleY : 1.0;
            foreach (var room in doc.Rooms)
            {
                sb.Append("<polygon points=\"");
                foreach (var p in room.Points)
                {
                    var px = p.X * scaleX;
                    var py = p.Y * scaleY;
                    sb.Append($"{px},{py} ");
                }
                sb.AppendLine("\" stroke='black' fill='lightgray' />");
            }

            foreach (var s in doc.Seats)
            {
                // seat positions stored in DIPs; convert to pixels
                var cx = (s.X + 8) * scaleX;
                var cy = (s.Y + 8) * scaleY;
                var r = 8 * Math.Max(scaleX, scaleY);
                sb.AppendLine($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='cornflowerblue' />");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        public void ExportToFile(ProjectDocument doc, string path)
        {
            var svg = ExportToSvg(doc);
            File.WriteAllText(path, svg);
        }
    }
}