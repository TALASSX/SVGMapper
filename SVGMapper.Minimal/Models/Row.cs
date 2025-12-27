using System.Collections.ObjectModel;

namespace SVGMapper.Minimal.Models
{
    public class Row
    {
        public System.Guid Id { get; set; } = System.Guid.NewGuid();
        public ObservableCollection<Seat> Seats { get; set; } = new();
        public double Spacing { get; set; } = 40.0;
    }
}