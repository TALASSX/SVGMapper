using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SVGMapper.Minimal.Models
{
    public class PolygonRoom : INotifyPropertyChanged
    {
        public System.Guid Id { get; set; } = System.Guid.NewGuid();
        public ObservableCollection<PointModel> Points { get; set; } = new();

        private string _name = "Room";
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}