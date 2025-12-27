using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using SVGMapper.Minimal.ViewModels;

namespace SVGMapper.Minimal
{
    public partial class MainWindow : Window
    {
        private bool _isDrawing = false;
        private Polyline? _preview;
        private Ellipse? _startMarker;
        private Polygon? _selectionOverlay;
        private Rectangle? _selectionBox;
        private Controls.RoomOverlayControl? _selectionControl;
        private TextBlock? _selectionLabel;
        private MainViewModel Vm => (MainViewModel)DataContext!;

        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += MainWindow_KeyDown;
            var vm = Vm;
            vm.PropertyChanged += Vm_PropertyChanged;
            this.Loaded += MainWindow_Loaded;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // draw full-line grid overlay
            DrawGrid();
            MainCanvas.SizeChanged += (s, ev) => DrawGrid();
        }

        private void DrawGrid()
        {
            // Remove previous grid lines
            var toRemove = new List<UIElement>();
            foreach (UIElement child in MainCanvas.Children)
            {
                if (child is Line l && l.Tag as string == "GridLine") toRemove.Add(l);
            }
            foreach (var el in toRemove) MainCanvas.Children.Remove(el);
            // if grid is disabled, we're done after removing any previous grid
            if (Vm != null && !Vm.GridVisible) return;

            double width = MainCanvas.ActualWidth > 0 ? MainCanvas.ActualWidth : MainCanvas.Width;
            double height = MainCanvas.ActualHeight > 0 ? MainCanvas.ActualHeight : MainCanvas.Height;
            double gridSize = Vm.GridSize;
            if (gridSize < 5) gridSize = 40;

            // Vertical lines
            for (double x = 0; x <= width; x += gridSize)
            {
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    Tag = "GridLine"
                };
                MainCanvas.Children.Add(line);
            }
            // Horizontal lines
            for (double y = 0; y <= height; y += gridSize)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1,
                    Tag = "GridLine"
                };
                MainCanvas.Children.Add(line);
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.BackgroundImageSource))
            {
                // compute DPI scale and update ViewModel so Canvas size maps to image pixels
                var src = PresentationSource.FromVisual(this);
                if (src != null)
                {
                    var m = src.CompositionTarget.TransformToDevice;
                    Vm.DpiScaleX = m.M11;
                    Vm.DpiScaleY = m.M22;
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.SelectedRoom))
            {
                // update overlay when selected room changes
                Dispatcher.Invoke(() => UpdateSelectionOverlay(Vm.SelectedRoom));
            }
            else if (e.PropertyName == nameof(MainViewModel.GridSize) || e.PropertyName == nameof(MainViewModel.GridVisible))
            {
                Dispatcher.Invoke(() => DrawGrid());
            }
        }


        private void MainWindow_KeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (Vm.SelectedRoom != null)
                    Vm.DeleteRoom(Vm.SelectedRoom);
            }
            else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_isDrawing)
                {
                    RevokeDraftPoint();
                }
                else
                {
                    Vm.UndoRedo.Undo();
                }
            }
            else if (e.Key == Key.Back)
            {
                // Backspace removes last draft point when drawing
                if (_isDrawing)
                {
                    RevokeDraftPoint();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                // Escape should cancel the current drawing and remove the start marker (undo if necessary)
                if (_isDrawing)
                {
                    // If the start marker was registered via UndoRedo, undo it so the visual is removed
                    if (_startMarker != null)
                    {
                        Vm.UndoRedo.Undo();
                    }

                    // Clear the draft in the viewmodel and clean up preview visuals
                    Vm.CancelPolygon();
                    CleanupDraftPreview();
                    e.Handled = true;
                }
            }
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDrawing)
            {
                RevokeDraftPoint();
            }
            else
            {
                Vm.UndoRedo.Undo();
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = GetCanvasPoint(e);
            if (Vm.IsPolygonTool)
            {
                if (!_isDrawing)
                {
                    _isDrawing = true;
                    _preview = new Polyline { Stroke = Brushes.Red, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 } };
                    MainCanvas.Children.Add(_preview);
                    Vm.StartPolygon();
                }

                // If double-click near starting point -> prompt to close with name
                if (e.ClickCount == 2 && _startMarker != null)
                {
                    var startCenter = new System.Windows.Point(Canvas.GetLeft(_startMarker) + _startMarker.Width / 2,
                                                              Canvas.GetTop(_startMarker) + _startMarker.Height / 2);
                    var dx = p.X - startCenter.X;
                    var dy = p.Y - startCenter.Y;
                    if (Math.Sqrt(dx * dx + dy * dy) <= 12)
                    {
                        // prompt for room name
                        var dlg = new InputDialog("Enter room name:", "Room");
                        dlg.Owner = this;
                        if (dlg.ShowDialog() == true)
                        {
                            Vm.ClosePolygon(dlg.ResponseText);
                        }
                        CleanupDraftPreview();
                        return;
                    }
                }

                Vm.AddPolygonPoint(p.X, p.Y);
                _preview!.Points.Add(p);

                // if this is the first point added, show start marker and register it in the undo stack
                if (_preview.Points.Count == 1)
                {
                    var el = new Ellipse { Width = 12, Height = 12, Stroke = Brushes.DarkRed, StrokeThickness = 2, Fill = Brushes.White };
                    // Use UndoRedo.Execute so the addition of the visual is undoable
                    Vm.UndoRedo.Execute(
                        doAction: () =>
                        {
                            Canvas.SetLeft(el, p.X - 6);
                            Canvas.SetTop(el, p.Y - 6);
                            MainCanvas.Children.Add(el);
                            _startMarker = el;
                        },
                        undoAction: () =>
                        {
                            MainCanvas.Children.Remove(el);
                            if (_startMarker == el) _startMarker = null;
                        }
                    );
                }
            }
            else if (Vm.IsSeatTool)
            {
                var snapped = Vm.Snap(p);
                Vm.AddSeat(snapped.X, snapped.Y);
            }
            else
            {
                // selection placeholder
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var p = GetCanvasPoint(e);
            if (_isDrawing && _preview != null)
            {
                // live preview — update last point
                if (_preview.Points.Count > 0)
                {
                    var last = _preview.Points[_preview.Points.Count - 1];
                    // leave existing, add temp point
                    if (_preview.Points.Count > Vm.CurrentDraftCount)
                    {
                        _preview.Points[_preview.Points.Count - 1] = p;
                    }
                    else
                    {
                        _preview.Points.Add(p);
                    }
                }
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // keep closing handled on MouseLeftButtonDown (for start-marker detection)
        }

        private System.Windows.Point GetCanvasPoint(InputEventArgs e)
        {
            System.Windows.Point p;
            if (e is MouseEventArgs me)
            {
                p = me.GetPosition(MainCanvas);
            }
            else if (e is MouseButtonEventArgs mbe)
            {
                p = mbe.GetPosition(MainCanvas);
            }
            else
            {
                p = new System.Windows.Point(0, 0);
            }

            // account for any RenderTransform on the canvas (e.g., zoom/scale)
            var rt = MainCanvas.RenderTransform;
            if (rt != null && !rt.Value.IsIdentity)
            {
                try
                {
                    var inv = rt.Inverse;
                    p = inv.Transform(p);
                }
                catch
                {
                    // ignore if not invertible
                }
            }

            // account for device DPI transform (convert from device pixels to DIPs if needed)
            var src = PresentationSource.FromVisual(this);
            if (src != null)
            {
                var m = src.CompositionTarget.TransformFromDevice;
                p = m.Transform(p);
            }

            return p;
        }

        private void CleanupDraftPreview()
        {
            _isDrawing = false;
            if (_preview != null)
            {
                MainCanvas.Children.Remove(_preview);
                _preview = null;
            }
            if (_startMarker != null)
            {
                MainCanvas.Children.Remove(_startMarker);
                _startMarker = null;
            }
        }

        private void RevokeDraftPoint()
        {
            if (!_isDrawing || _preview == null) return;
            // update viewmodel draft
            Vm.RevokeLastPoint();

            // sync preview polyline with current draft points
            var draftCount = Vm.CurrentDraftCount;
            // remove any temporary point at end used for mouse
            while (_preview.Points.Count > draftCount)
            {
                _preview.Points.RemoveAt(_preview.Points.Count - 1);
            }

            // if still have points, ensure preview matches
            if (_preview.Points.Count > draftCount)
            {
                _preview.Points.RemoveAt(_preview.Points.Count - 1);
            }

            if (draftCount == 0)
            {
                // no points left: keep drawing state but remove start marker
                if (_startMarker != null)
                {
                    MainCanvas.Children.Remove(_startMarker);
                    _startMarker = null;
                }
            }
        }

        private void Room_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Polygon pl && pl.DataContext is Models.PolygonRoom room)
            {
                Vm.SelectedRoom = room;
                e.Handled = true;
            }
        }

        private void Room_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Polygon pl && pl.DataContext is Models.PolygonRoom room)
            {
                Vm.SelectedRoom = room;
                e.Handled = false; // allow context menu to open
            }
        }

        private void UpdateSelectionOverlay(Models.PolygonRoom? room)
        {
            // remove existing overlay and box
            if (_selectionOverlay != null)
            {
                MainCanvas.Children.Remove(_selectionOverlay);
                _selectionOverlay = null;
            }
            if (_selectionLabel != null)
            {
                MainCanvas.Children.Remove(_selectionLabel);
                _selectionLabel = null;
            }
            if (_selectionBox != null)
            {
                MainCanvas.Children.Remove(_selectionBox);
                _selectionBox = null;
            }
            if (_selectionControl != null)
            {
                MainCanvas.Children.Remove(_selectionControl);
                _selectionControl = null;
            }

            if (room == null) return;
            // locate the original UI element for this room (Polyline/Polygon/Path)
            FrameworkElement? originalEl = null;
            foreach (var child in MainCanvas.Children)
            {
                if (child is FrameworkElement fe && fe.DataContext == room)
                {
                    originalEl = fe;
                    break;
                }
            }

            // If original is a Path (SVG path), create a Path overlay; otherwise use Polygon
            if (originalEl is System.Windows.Shapes.Path origPath)
            {
                var overlayPath = new System.Windows.Shapes.Path
                {
                    Data = origPath.Data?.Clone(),
                    Stroke = Brushes.Orange,
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(60, 255, 165, 0)),
                    IsHitTestVisible = false
                };

                // copy transform and position
                try { if (origPath.RenderTransform != null) overlayPath.RenderTransform = origPath.RenderTransform.Clone(); } catch { }
                var left = Canvas.GetLeft(origPath);
                var top = Canvas.GetTop(origPath);
                Canvas.SetLeft(overlayPath, double.IsNaN(left) ? 0 : left);
                Canvas.SetTop(overlayPath, double.IsNaN(top) ? 0 : top);

                _selectionOverlay = null; // keep _selectionOverlay typed as Polygon null
                MainCanvas.Children.Add(overlayPath);
                Canvas.SetZIndex(overlayPath, 998);

                // compute bounds for overlay control placement
                var pb = overlayPath.Data?.Bounds ?? System.Windows.Rect.Empty;
                if (pb != System.Windows.Rect.Empty)
                {
                    double pad = 4;
                    var control = new Controls.RoomOverlayControl();
                    control.Room = room;
                    control.Width = Math.Max(24, pb.Width + pad * 2);
                    control.Height = Math.Max(24, pb.Height + pad * 2);
                    MainCanvas.Children.Add(control);
                    Canvas.SetLeft(control, pb.X - pad);
                    Canvas.SetTop(control, pb.Y - pad);
                    Canvas.SetZIndex(control, 999);
                    _selectionControl = control;

                    // label handled by control itself; keep lightweight label as fallback
                }
                return;
            }

            // Fallback: build polygon overlay from model points
            var poly = new Polygon
            {
                Stroke = Brushes.Orange,
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Color.FromArgb(60, 255, 165, 0)),
                IsHitTestVisible = false
            };

            foreach (var p in room.Points)
            {
                poly.Points.Add(new System.Windows.Point(p.X, p.Y));
            }

            // if original element exists, copy its position and transform to avoid offsets/scale mismatches
            if (originalEl != null)
            {
                try { if (originalEl.RenderTransform != null) poly.RenderTransform = originalEl.RenderTransform.Clone(); } catch { }
                var left = Canvas.GetLeft(originalEl);
                var top = Canvas.GetTop(originalEl);
                Canvas.SetLeft(poly, double.IsNaN(left) ? 0 : left);
                Canvas.SetTop(poly, double.IsNaN(top) ? 0 : top);
            }

            _selectionOverlay = poly;
            MainCanvas.Children.Add(_selectionOverlay);
            Canvas.SetZIndex(_selectionOverlay, 999);

            // create bounding box overlay
            System.Windows.Rect bounds;
            if (_selectionOverlay.RenderedGeometry != null)
            {
                bounds = _selectionOverlay.RenderedGeometry.Bounds;
            }
            else if (_selectionOverlay.Points.Count > 0)
            {
                double minX = double.PositiveInfinity, minY = double.PositiveInfinity, maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
                foreach (var pt in _selectionOverlay.Points)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
                bounds = new System.Windows.Rect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
            }
            else
            {
                bounds = System.Windows.Rect.Empty;
            }

            if (bounds != System.Windows.Rect.Empty)
            {
                double pad = 4;
                var control = new Controls.RoomOverlayControl();
                control.Room = room;
                control.Width = Math.Max(24, bounds.Width + pad * 2);
                control.Height = Math.Max(24, bounds.Height + pad * 2);
                MainCanvas.Children.Add(control);
                Canvas.SetLeft(control, bounds.X - pad);
                Canvas.SetTop(control, bounds.Y - pad);
                Canvas.SetZIndex(control, 999);
                _selectionControl = control;
            }
        }

        private System.Windows.Point ComputeCentroid(Models.PolygonRoom room)
        {
            double cx = 0, cy = 0;
            int n = room.Points.Count;
            if (n == 0) return new System.Windows.Point(0, 0);
            // use simple average (sufficient for label placement)
            foreach (var p in room.Points)
            {
                cx += p.X; cy += p.Y;
            }
            return new System.Windows.Point(cx / n, cy / n);
        }
    }
}