namespace SVGMapper.Minimal.Models
{
    public class Seat
    {
        public System.Guid Id { get; set; } = System.Guid.NewGuid();
        public string Label { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
    }
}