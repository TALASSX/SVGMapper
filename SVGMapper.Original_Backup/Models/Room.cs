using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SVGMapper.Models
{
    public class Room : SelectableBase
    {
        public string Name { get; set; } = string.Empty;
        public List<Point> Points { get; set; } = new List<Point>();

        private Color _strokeColor = Colors.Red;
        public Color StrokeColor
        {
            get => _strokeColor;
            set { if (_strokeColor == value) return; _strokeColor = value; OnPropertyChanged(nameof(StrokeColor)); }
        }

        private Color _fillColor = Color.FromArgb(24, 255, 0, 0);
        public Color FillColor
        {
            get => _fillColor;
            set { if (_fillColor == value) return; _fillColor = value; OnPropertyChanged(nameof(FillColor)); }
        }

        private double _opacity = 1.0;
        public double Opacity
        {
            get => _opacity;
            set { if (_opacity == value) return; _opacity = value; OnPropertyChanged(nameof(Opacity)); }
        }

        public Point Center
        {
            get
            {
                if (Points.Count == 0) return new Point(0, 0);
                double sx = 0, sy = 0;
                foreach (var p in Points) { sx += p.X; sy += p.Y; }
                return new Point(sx / Points.Count, sy / Points.Count);
            }
        }

        /// <summary>
        /// Replace the point list and raise change notification so views may update.
        /// </summary>
        public void SetPoints(List<Point> newPoints)
        {
            Points = newPoints;
            OnPropertyChanged(nameof(Points));
        }
    }
}