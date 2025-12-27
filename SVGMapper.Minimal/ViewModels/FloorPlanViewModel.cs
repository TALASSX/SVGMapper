using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SVGMapper.Minimal.ViewModels
{
    public class FloorPlanViewModel : INotifyPropertyChanged
    {
        // Placeholder for separation — minimal implementation uses MainViewModel directly
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}