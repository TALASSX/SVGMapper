using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SVGMapper.Models;

namespace SVGMapper.Services
{
    public static class SvgExportService
    {
        public static void Export(string path, IEnumerable<Room> rooms, IEnumerable<Seat> seats, string? backgroundSvg = null, string? backgroundImagePath = null)
        {
            var svgNs = "http://www.w3.org/2000/svg";
            var svg = new XElement(XName.Get("svg", svgNs),
                new XAttribute("xmlns", svgNs)
            );

            // Include background SVG inline if available
            if (!string.IsNullOrEmpty(backgroundSvg))
            {
                try
                {
                    var bgDoc = XDocument.Parse(backgroundSvg);
                    var bgRoot = bgDoc.Root;
                    if (bgRoot != null)
                    {
                        var g = new XElement(XName.Get("g", svgNs), new XAttribute("id", "background"));
                        foreach (var el in bgRoot.Elements()) g.Add(el);
                        svg.Add(g);
                    }
                }
                catch
                {
                    // ignore parse errors
                }
            }

            // If a raster path is provided, embed it as a data URI image
            if (!string.IsNullOrEmpty(backgroundImagePath) && System.IO.File.Exists(backgroundImagePath))
            {
                try
                {
                    var bytes = System.IO.File.ReadAllBytes(backgroundImagePath);
                    var ext = System.IO.Path.GetExtension(backgroundImagePath).ToLowerInvariant().TrimStart('.');
                    var mime = ext == "png" ? "image/png" : (ext == "jpg" || ext == "jpeg" ? "image/jpeg" : "application/octet-stream");
                    var b64 = System.Convert.ToBase64String(bytes);
                    var href = $"data:{mime};base64,{b64}";
                    var img = new XElement(XName.Get("image", svgNs),
                        new XAttribute(XName.Get("href", "http://www.w3.org/1999/xlink"), href),
                        new XAttribute("x", 0),
                        new XAttribute("y", 0)
                    );
                    svg.Add(img);
                }
                catch
                {
                    // ignore
                }
            }

            // Place rooms in a "rooms" group layer
            var roomsGroup = new XElement(XName.Get("g", svgNs), new XAttribute("id", "rooms"));
            foreach (var r in rooms)
            {
                var points = string.Join(" ", r.Points.Select(p => $"{p.X},{p.Y}"));

                string StrokeColorToHex(System.Windows.Media.Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

                var poly = new XElement(XName.Get("polygon", svgNs),
                    new XAttribute("points", points),
                    new XAttribute("fill", StrokeColorToHex(r.FillColor)),
                    new XAttribute("stroke", StrokeColorToHex(r.StrokeColor)),
                    new XAttribute("stroke-width", 2),
                    new XAttribute("fill-opacity", r.Opacity),
                    new XAttribute("data-name", r.Name ?? string.Empty));

                roomsGroup.Add(poly);

                // label as <text>
                var center = r.Center;
                var text = new XElement(XName.Get("text", svgNs), r.Name,
                    new XAttribute("x", center.X),
                    new XAttribute("y", center.Y),
                    new XAttribute("text-anchor", "middle"));
                roomsGroup.Add(text);
            }

            // Place seats in groups by row
            var seatsGroup = new XElement(XName.Get("g", svgNs), new XAttribute("id", "seats"));

            var seatsByRow = seats.GroupBy(s => s.Row ?? string.Empty);
            foreach (var grp in seatsByRow)
            {
                if (!string.IsNullOrEmpty(grp.Key))
                {
                    var rowGroup = new XElement(XName.Get("g", svgNs), new XAttribute("id", "row-" + grp.Key));
                    foreach (var s in grp)
                    {
                        var r = new XElement(XName.Get("circle", svgNs),
                            new XAttribute("cx", s.Position.X),
                            new XAttribute("cy", s.Position.Y),
                            new XAttribute("r", s.Size / 2.0),
                            new XAttribute("fill", $"#{s.Color.R:X2}{s.Color.G:X2}{s.Color.B:X2}"),
                            new XAttribute("data-seat", s.SeatNumber ?? string.Empty));

                        var t = new XElement(XName.Get("text", svgNs), s.SeatNumber,
                            new XAttribute("x", s.Position.X),
                            new XAttribute("y", s.Position.Y + (s.Size / 2.0) + 12),
                            new XAttribute("font-size", 12),
                            new XAttribute("text-anchor", "middle"));

                        rowGroup.Add(r);
                        rowGroup.Add(t);
                    }
                    seatsGroup.Add(rowGroup);
                }
                else
                {
                    foreach (var s in grp)
                    {
                        var r = new XElement(XName.Get("circle", svgNs),
                            new XAttribute("cx", s.Position.X),
                            new XAttribute("cy", s.Position.Y),
                            new XAttribute("r", s.Size / 2.0),
                            new XAttribute("fill", $"#{s.Color.R:X2}{s.Color.G:X2}{s.Color.B:X2}"),
                            new XAttribute("data-seat", s.SeatNumber ?? string.Empty));

                        var t = new XElement(XName.Get("text", svgNs), s.SeatNumber,
                            new XAttribute("x", s.Position.X),
                            new XAttribute("y", s.Position.Y + (s.Size / 2.0) + 12),
                            new XAttribute("font-size", 12),
                            new XAttribute("text-anchor", "middle"));

                        seatsGroup.Add(r);
                        seatsGroup.Add(t);
                    }
                }
            }

            svg.Add(roomsGroup);
            svg.Add(seatsGroup);

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "no"), svg);
            File.WriteAllText(path, doc.ToString());
        }
    }
}