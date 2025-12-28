using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;
using SVGMapper.Minimal.Models;

namespace SVGMapper.Minimal.Services
{
    public class SvgExportService
    {
        public string ExportToSvg(ProjectDocument doc)
        {
            var sb = new StringBuilder();

            // For SVG export we use image pixel coordinates directly (viewBox in image px units)
            var scaleX = 1.0;
            var scaleY = 1.0;

            // Determine base viewBox size (use embedded image size when present)
            var hasImage = doc.BackgroundImageWidth > 0 && doc.BackgroundImageHeight > 0;
            double vbW = hasImage ? doc.BackgroundImageWidth : 2000;
            double vbH = hasImage ? doc.BackgroundImageHeight : 1400;

            // Compute geometry extents (in image pixel space)
            double minXpx = double.PositiveInfinity, minYpx = double.PositiveInfinity, maxXpx = double.NegativeInfinity, maxYpx = double.NegativeInfinity;
            foreach (var room in doc.Rooms)
            {
                if (room.NormalizedPoints != null && room.NormalizedPoints.Count > 0 && hasImage)
                {
                    foreach (var n in room.NormalizedPoints)
                    {
                        var px = n.X * doc.BackgroundImageWidth;
                        var py = n.Y * doc.BackgroundImageHeight;
                        minXpx = Math.Min(minXpx, px);
                        minYpx = Math.Min(minYpx, py);
                        maxXpx = Math.Max(maxXpx, px);
                        maxYpx = Math.Max(maxYpx, py);
                    }
                }
                else
                {
                    foreach (var p in room.Points)
                    {
                        var px = p.X;
                        var py = p.Y;
                        minXpx = Math.Min(minXpx, px);
                        minYpx = Math.Min(minYpx, py);
                        maxXpx = Math.Max(maxXpx, px);
                        maxYpx = Math.Max(maxYpx, py);
                    }
                }
            }
            foreach (var s in doc.Seats)
            {
                var sx = (s.X + 8) * scaleX; var sy = (s.Y + 8) * scaleY; // seat center
                minXpx = Math.Min(minXpx, sx);
                minYpx = Math.Min(minYpx, sy);
                maxXpx = Math.Max(maxXpx, sx);
                maxYpx = Math.Max(maxYpx, sy);
            }

            if (!double.IsInfinity(minXpx) && !double.IsInfinity(minYpx) && !double.IsInfinity(maxXpx) && !double.IsInfinity(maxYpx))
            {
                vbW = Math.Max(vbW, Math.Ceiling(maxXpx));
                vbH = Math.Max(vbH, Math.Ceiling(maxYpx));
            }

            if (vbW <= 0) vbW = 2000;
            if (vbH <= 0) vbH = 1400;

            sb.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {vbW} {vbH}' width='{vbW}' height='{vbH}'>");

            // embed background image if present
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
                    var iw = vbW.ToString(CultureInfo.InvariantCulture);
                    var ih = vbH.ToString(CultureInfo.InvariantCulture);
                    sb.AppendLine($"<image href=\"data:{mime};base64,{base64}\" x=\"0\" y=\"0\" width='{iw}' height='{ih}' preserveAspectRatio=\"xMidYMid meet\" style='pointer-events:none' />");
                }
                catch
                {
                    // ignore embed errors
                }
            }

            // Export grid lines and numbers for visual comparison
            double gridSizeSvg = 40; // matches default UI grid size
            for (double x = 0; x <= vbW; x += gridSizeSvg)
            {
                sb.AppendLine($"<line x1='{x.ToString(CultureInfo.InvariantCulture)}' y1='0' x2='{x.ToString(CultureInfo.InvariantCulture)}' y2='{vbH.ToString(CultureInfo.InvariantCulture)}' stroke='lightgray' stroke-width='1' />");
                sb.AppendLine($"<text x='{(x + 2).ToString(CultureInfo.InvariantCulture)}' y='12' font-size='10' fill='gray'>{(int)x}</text>");
            }
            for (double y = 0; y <= vbH; y += gridSizeSvg)
            {
                sb.AppendLine($"<line x1='0' y1='{y.ToString(CultureInfo.InvariantCulture)}' x2='{vbW.ToString(CultureInfo.InvariantCulture)}' y2='{y.ToString(CultureInfo.InvariantCulture)}' stroke='lightgray' stroke-width='1' />");
                sb.AppendLine($"<text x='2' y='{(y + 12).ToString(CultureInfo.InvariantCulture)}' font-size='10' fill='gray'>{(int)y}</text>");
            }

            // polygons
            int idx = 1;
            foreach (var room in doc.Rooms)
            {
                var label = (room.FieldNumber ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(label)) label = (room.Name ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(label)) label = idx.ToString();

                sb.AppendLine($"<g data-label='{System.Security.SecurityElement.Escape(label)}' id='room-{room.Id}' class='room'>");
                sb.Append("  <polygon points=\"");
                if (room.NormalizedPoints != null && room.NormalizedPoints.Count > 0 && hasImage)
                {
                    foreach (var n in room.NormalizedPoints)
                    {
                        var px = n.X * doc.BackgroundImageWidth;
                        var py = n.Y * doc.BackgroundImageHeight;
                        sb.Append($"{px.ToString(CultureInfo.InvariantCulture)},{py.ToString(CultureInfo.InvariantCulture)} ");
                    }
                }
                else
                {
                    foreach (var p in room.Points)
                    {
                        var px = p.X;
                        var py = p.Y;
                        sb.Append($"{px.ToString(CultureInfo.InvariantCulture)},{py.ToString(CultureInfo.InvariantCulture)} ");
                    }
                }
                sb.AppendLine("\" stroke='black' />");
                sb.AppendLine("</g>");
                idx++;
            }

            // seats
            foreach (var s in doc.Seats)
            {
                var cxPx = (s.X + 8) * scaleX;
                var cyPx = (s.Y + 8) * scaleY;
                var rPx = 8 * Math.Max(scaleX, scaleY);
                sb.AppendLine($"<circle cx='{cxPx.ToString(CultureInfo.InvariantCulture)}' cy='{cyPx.ToString(CultureInfo.InvariantCulture)}' r='{rPx.ToString(CultureInfo.InvariantCulture)}' fill='cornflowerblue' />");
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