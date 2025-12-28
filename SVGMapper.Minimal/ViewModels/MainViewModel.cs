using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SVGMapper.Minimal.Models;
using SVGMapper.Minimal.Services;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;

namespace SVGMapper.Minimal.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PolygonRoom> Rooms { get; } = new();
        public ObservableCollection<Seat> Seats { get; } = new();

        public ProjectDocument Document { get; } = new();
        private string? _backgroundImagePath;
        public string? BackgroundImagePath { get => _backgroundImagePath; set { _backgroundImagePath = value; OnPropertyChanged(); } }

        private BitmapImage? _backgroundImageSource;
        public BitmapImage? BackgroundImageSource { get => _backgroundImageSource; set { _backgroundImageSource = value; OnPropertyChanged(); } }

        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;
        public double DpiScaleX { get => _dpiScaleX; set { _dpiScaleX = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanvasWidth)); OnPropertyChanged(nameof(CanvasHeight)); } }
        public double DpiScaleY { get => _dpiScaleY; set { _dpiScaleY = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanvasWidth)); OnPropertyChanged(nameof(CanvasHeight)); } }

        public double CanvasWidth => Document.BackgroundImageWidth > 0 ? (Document.BackgroundImageWidth / DpiScaleX) : 2000;
        public double CanvasHeight => Document.BackgroundImageHeight > 0 ? (Document.BackgroundImageHeight / DpiScaleY) : 1400;

        public SnapService SnapService { get; } = new();
        public UndoRedoService UndoRedo { get; } = new();
        public SvgExportService Exporter { get; } = new();

        private bool _isPolygonTool;
        public bool IsPolygonTool { get => _isPolygonTool; set { _isPolygonTool = value; OnPropertyChanged(); } }
        private bool _isSeatTool;
        public bool IsSeatTool { get => _isSeatTool; set { _isSeatTool = value; OnPropertyChanged(); } }

        private PolygonRoom? _currentDraft;
        public int CurrentDraftCount => _currentDraft?.Points.Count ?? 0;

        private PolygonRoom? _selectedRoom;
        public PolygonRoom? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (_selectedRoom != null) _selectedRoom.IsSelected = false;
                _selectedRoom = value;
                if (_selectedRoom != null) _selectedRoom.IsSelected = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedRoomNumber));
            }
        }

        public int SelectedRoomNumber => SelectedRoom != null ? (Rooms.IndexOf(SelectedRoom) + 1) : 0;

        public MainViewModel()
        {
            Document.Rooms = Rooms;
            Document.Seats = Seats;
            Rooms.CollectionChanged += (s, e) => OnPropertyChanged(nameof(SelectedRoomNumber));
        }

        public ICommand SelectToolCommand => new RelayCommand(_ => { IsPolygonTool = false; IsSeatTool = false; });
        public ICommand PolygonToolCommand => new RelayCommand(_ => { IsPolygonTool = true; IsSeatTool = false; });
        public ICommand SeatToolCommand => new RelayCommand(_ => { IsSeatTool = true; IsPolygonTool = false; });
        public ICommand RowToolCommand => new RelayCommand(_ => { /* not implemented in minimal */ });
        public ICommand ExportCommand => new RelayCommand(_ =>
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.Filter = "SVG files|*.svg|All files|*.*";
                dlg.FileName = "export.svg";
                if (dlg.ShowDialog() == true)
                {
                    Exporter.ExportToFile(Document, dlg.FileName);
                    System.Windows.MessageBox.Show($"Exported to {dlg.FileName}", "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        });
        public ICommand DeleteRoomCommand => new RelayCommand(p =>
        {
            if (p is Models.PolygonRoom room) DeleteRoom(room);
            else if (SelectedRoom != null) DeleteRoom(SelectedRoom);
        });
        public ICommand ImportImageCommand => new RelayCommand(_ => ImportImage());
        public ICommand UndoCommand => new RelayCommand(_ => UndoRedo.Undo());
        public ICommand RevokePointCommand => new RelayCommand(_ => RevokeLastPoint());

        private bool _gridVisible = true;
        public bool GridVisible { get => _gridVisible; set { _gridVisible = value; OnPropertyChanged(); } }
        private double _gridSize = 40.0;
        public double GridSize { get => _gridSize; set { _gridSize = value; OnPropertyChanged(); } }

        public void StartPolygon()
        {
            _currentDraft = new PolygonRoom();
        }

        public void AddPolygonPoint(double x, double y)
        {
            if (_currentDraft == null) return;
            var p = SnapService.Snap(x, y, GridSize);
            _currentDraft.Points.Add(new PointModel { X = p.X, Y = p.Y });
            OnPropertyChanged(nameof(CurrentDraftCount));
        }

        public void RevokeLastPoint()
        {
            if (_currentDraft == null) return;
            if (_currentDraft.Points.Count == 0) return;
            _currentDraft.Points.RemoveAt(_currentDraft.Points.Count - 1);
            OnPropertyChanged(nameof(CurrentDraftCount));
        }

        public void ClosePolygon()
        {
            ClosePolygon(null);
        }

        public void ClosePolygon(string? name)
        {
            if (_currentDraft == null) return;
            if (_currentDraft.Points.Count < 3) { _currentDraft = null; return; }
            if (!string.IsNullOrWhiteSpace(name)) _currentDraft.Name = name!;
            var draft = _currentDraft;
            // compute normalized points based on document image size (store fractions)
            if (Document != null && Document.BackgroundImageWidth > 0 && Document.BackgroundImageHeight > 0)
            {
                draft.NormalizedPoints.Clear();
                foreach (var p in draft.Points)
                {
                    draft.NormalizedPoints.Add(new PointModel { X = p.X / Document.BackgroundImageWidth, Y = p.Y / Document.BackgroundImageHeight });
                }
            }
            UndoRedo.Execute(() => Rooms.Add(draft), () => Rooms.Remove(draft));
            _currentDraft = null;
        }

        public void CancelPolygon()
        {
            _currentDraft = null;
            OnPropertyChanged(nameof(CurrentDraftCount));
        }

        public System.Windows.Point? GetCurrentDraftFirstPoint()
        {
            if (_currentDraft == null || _currentDraft.Points.Count == 0) return null;
            var p = _currentDraft.Points[0];
            return new System.Windows.Point(p.X, p.Y);
        }

        public void DeleteRoom(PolygonRoom room)
        {
            if (room == null) return;
            var idx = Rooms.IndexOf(room);
            UndoRedo.Execute(
                () => Rooms.Remove(room),
                () => Rooms.Insert(idx >= 0 ? idx : Rooms.Count, room));
            if (SelectedRoom == room) SelectedRoom = null;
        }

        private void ImportImage()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.gif;*.bmp|All files|*.*";
            if (dlg.ShowDialog() == true)
            {
                BackgroundImagePath = dlg.FileName;
                try
                {
                    var bi = new BitmapImage();
                    using (var fs = File.OpenRead(BackgroundImagePath))
                    {
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = fs;
                        bi.EndInit();
                        bi.Freeze();
                    }
                    BackgroundImageSource = bi;
                    Document.BackgroundImagePath = BackgroundImagePath;

                    // store pixel dimensions
                    Document.BackgroundImageWidth = bi.PixelWidth;
                    Document.BackgroundImageHeight = bi.PixelHeight;

                    // compute DPI scale using image's DPI (image DPI / 96)
                    var imgScaleX = bi.DpiX > 0 ? (bi.DpiX / 96.0) : 1.0;
                    var imgScaleY = bi.DpiY > 0 ? (bi.DpiY / 96.0) : 1.0;
                    Document.BackgroundDpiScaleX = imgScaleX;
                    Document.BackgroundDpiScaleY = imgScaleY;

                    // notify canvas size (CanvasWidth/Height use pixel / DPI scale)
                    OnPropertyChanged(nameof(CanvasWidth));
                    OnPropertyChanged(nameof(CanvasHeight));
                }
                catch
                {
                    BackgroundImageSource = null;
                    Document.BackgroundImagePath = null;
                }
            }
        }

        public void AddSeat(double x, double y)
        {
            var p = SnapService.Snap(x, y, GridSize);
            var seat = new Seat { X = p.X - 8, Y = p.Y - 8, Label = $"S{Seats.Count + 1}" };
            Seats.Add(seat);
            UndoRedo.Execute(() => Seats.Add(seat), () => Seats.Remove(seat));
        }

        public System.Windows.Point Snap(System.Windows.Point p)
        {
            var r = SnapService.Snap(p.X, p.Y, GridSize);
            return new System.Windows.Point(r.X, r.Y);
        }

        public object? Selected { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}