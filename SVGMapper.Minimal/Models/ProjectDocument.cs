using System.Collections.ObjectModel;

namespace SVGMapper.Minimal.Models
{
    public class ProjectDocument
    {
        public ObservableCollection<PolygonRoom> Rooms { get; set; } = new();
        public ObservableCollection<Seat> Seats { get; set; } = new();
        public ObservableCollection<Row> Rows { get; set; } = new();
        public string? BackgroundImagePath { get; set; }
        public int BackgroundImageWidth { get; set; }
        public int BackgroundImageHeight { get; set; }
        public double BackgroundDpiScaleX { get; set; } = 1.0;
        public double BackgroundDpiScaleY { get; set; } = 1.0;
    }
}