using System.Windows;
using System.Windows.Media;

namespace SVGMapper.Models
{
    public class Seat : SelectableBase
    {
        public string SeatNumber { get; set; } = string.Empty;
        public Point Position { get; set; }
        public string Row { get; set; } = string.Empty;

        private Color _color = Colors.SteelBlue;
        public Color Color
        {
            get => _color;
            set { if (_color == value) return; _color = value; OnPropertyChanged(nameof(Color)); }
        }

        private double _size = 20;
        public double Size
        {
            get => _size;
            set { if (_size == value) return; _size = value; OnPropertyChanged(nameof(Size)); }
        }
    }
}