using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace SVGMapper.Services
{
    /// <summary>
    /// Central selection service for tools and views.
    /// Supports single / multi selection and exposes SelectedItem for inspector binding.
    /// </summary>
    public class SelectionService : INotifyPropertyChanged
    {
        public ObservableCollection<object> SelectedItems { get; } = new();

        public object? SelectedItem => SelectedItems.Count > 0 ? SelectedItems[0] : null;

        public event Action? SelectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Clear()
        {
            foreach (var item in SelectedItems.ToList())
                SetSelectedFlag(item, false);
            SelectedItems.Clear();
            SelectionChanged?.Invoke();
            OnPropertyChanged(nameof(SelectedItem));
        }

        public void Select(object item)
        {
            Clear();
            SelectedItems.Add(item);
            SetSelectedFlag(item, true);
            SelectionChanged?.Invoke();
            OnPropertyChanged(nameof(SelectedItem));
        }

        public void Add(object item)
        {
            if (!SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
                SetSelectedFlag(item, true);
                SelectionChanged?.Invoke();
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public void Toggle(object item)
        {
            if (SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);
                SetSelectedFlag(item, false);
            }
            else
            {
                SelectedItems.Add(item);
                SetSelectedFlag(item, true);
            }
            SelectionChanged?.Invoke();
            OnPropertyChanged(nameof(SelectedItem));
        }

        private void SetSelectedFlag(object item, bool value)
        {
            // Try to set an IsSelected property if present
            var prop = item.GetType().GetProperty("IsSelected");
            if (prop != null && prop.CanWrite)
                prop.SetValue(item, value);
        }
    }
}