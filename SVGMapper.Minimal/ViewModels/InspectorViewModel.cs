using System.ComponentModel;
using System.Runtime.CompilerServices;
using SVGMapper.Minimal.Models;

namespace SVGMapper.Minimal.ViewModels
{
    public class InspectorViewModel : INotifyPropertyChanged
    {
        private object? _selected;
        public object? Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }

        public string? Name
        {
            get => Selected switch
            {
                PolygonRoom p => p.Name,
                _ => null
            };
            set
            {
                if (Selected is PolygonRoom p && value != null) { p.Name = value; OnPropertyChanged(nameof(Name)); }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}