using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SVGMapper.Minimal.Models
{
    public class PolygonRoom : INotifyPropertyChanged
    {
        public System.Guid Id { get; set; } = System.Guid.NewGuid();
        public ObservableCollection<PointModel> Points { get; set; } = new();
        // Normalized points in [0..1] relative to the original image pixel size.
        // X = fraction across width, Y = fraction down height.
        public ObservableCollection<PointModel> NormalizedPoints { get; set; } = new();

        private string _name = "Room";
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private string? _fieldNumber;
        // Optional explicit field number used for exporting as data-label
        public string? FieldNumber { get => _fieldNumber; set { _fieldNumber = value; OnPropertyChanged(); } }

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}