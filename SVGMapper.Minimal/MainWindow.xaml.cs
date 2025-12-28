using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using SVGMapper.Minimal.ViewModels;
using SVGMapper.Minimal.Models;

namespace SVGMapper.Minimal
{
    public partial class MainWindow : Window
    {
        private void EnsurePointsFromNormalized(Models.PolygonRoom room, System.Windows.Media.Imaging.BitmapSource? bmp)
        {
            if (room == null || bmp == null) return;
            if ((room.Points == null || room.Points.Count == 0) && room.NormalizedPoints != null && room.NormalizedPoints.Count > 0)
            {
                room.Points.Clear();
                foreach (var n in room.NormalizedPoints)
                {
                    room.Points.Add(new Models.PointModel { X = n.X * bmp.PixelWidth, Y = n.Y * bmp.PixelHeight });
                }
            }
        }
        // Draw all polygons in code-behind for perfect alignment
        private void DrawPolygonsOverlay()
        {
            if (ImageOverlay == null || Vm?.Document?.Rooms == null || BgImage.Source == null)
                return;
            ImageOverlay.Children.Clear();
            var bmp = BgImage.Source as System.Windows.Media.Imaging.BitmapSource;
            if (bmp == null) return;
            var imgPxSize = new System.Windows.Size(bmp.PixelWidth, bmp.PixelHeight);
            var controlSize = new System.Windows.Size(BgImage.ActualWidth, BgImage.ActualHeight);
            foreach (var room in Vm.Document.Rooms)
            {
                EnsurePointsFromNormalized(room, bmp);
                var poly = new Polygon
                {
                    Fill = new SolidColorBrush(Color.FromArgb(128, 211, 211, 211)), // LightGray, 50% opacity
                    Stroke = room.IsSelected ? Brushes.Orange : Brushes.Black,
                    StrokeThickness = room.IsSelected ? 3 : 2,
                    Opacity = 0.6,
                    Tag = room
                };
                IEnumerable<System.Windows.Point> sourcePts;
                if (room.NormalizedPoints != null && room.NormalizedPoints.Count > 0)
                {
                    // denormalize to image pixels
                    sourcePts = room.NormalizedPoints.Select(n => new System.Windows.Point(n.X * imgPxSize.Width, n.Y * imgPxSize.Height));
                }
                else
                {
                    sourcePts = room.Points.Select(p => new System.Windows.Point(p.X, p.Y));
                }
                foreach (var pt in sourcePts)
                {
                    var mapped = SVGMapper.Minimal.Services.ImageCoordinateTransformer.TransformPoint(
                        new System.Windows.Point(pt.X, pt.Y), imgPxSize, controlSize, bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0, bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0,
                        SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);
                    poly.Points.Add(mapped);
                }
                poly.MouseLeftButtonDown += Room_MouseLeftButtonDown;
                poly.MouseRightButtonDown += Room_MouseRightButtonDown;
                ImageOverlay.Children.Add(poly);
            }
        }

        private bool _isDrawing = false;
        private Polyline? _preview;
        private Ellipse? _startMarker;
        private Polygon? _selectionOverlay;
        private Rectangle? _selectionBox;
        private Controls.RoomOverlayControl? _selectionControl;
        private TextBlock? _selectionLabel;

        // Vertex editing support
        private List<Ellipse> _vertexHandles = new();
        private int? _selectedVertexIndex = null;
        private int? _draggingVertexIndex = null;
        private Models.PolygonRoom? _draggingRoom = null;
        private System.Windows.Point? _dragOriginalPoint = null;

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
            // redraw grid when the image display size changes
            BgImage.SizeChanged += (s, ev) => DrawGrid();
            // also redraw if the canvas itself resizes (fallback)
            MainCanvas.SizeChanged += (s, ev) => DrawGrid();

            // draw polygons overlay when image or rooms change
            BgImage.SizeChanged += (s, ev) => DrawPolygonsOverlay();
            Vm.PropertyChanged += (s, ev) => { if (ev.PropertyName == nameof(Vm.Rooms)) DrawPolygonsOverlay(); };
            Vm.Document.Rooms.CollectionChanged += (s, ev) => DrawPolygonsOverlay();
            DrawPolygonsOverlay();
        }

        private void DrawGrid()
        {
            // Remove previous grid lines and numbers from the image overlay
            if (ImageOverlay != null)
            {
                var toRemove = new List<UIElement>();
                foreach (UIElement child in ImageOverlay.Children)
                {
                    if ((child is Line l && l.Tag as string == "GridLine") || (child is TextBlock t && t.Tag as string == "GridNumber")) toRemove.Add(child);
                }
                foreach (var el in toRemove) ImageOverlay.Children.Remove(el);
            }
            // if grid is disabled, we're done after removing any previous grid
            if (Vm != null && !Vm.GridVisible) return;

            double width = ImageOverlay != null && ImageOverlay.ActualWidth > 0 ? ImageOverlay.ActualWidth : (MainCanvas.ActualWidth > 0 ? MainCanvas.ActualWidth : MainCanvas.Width);
            double height = ImageOverlay != null && ImageOverlay.ActualHeight > 0 ? ImageOverlay.ActualHeight : MainCanvas.Height;
            double gridSize = Vm.GridSize;
            if (gridSize < 5) gridSize = 40;
            // Vertical lines and numbers
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
                if (ImageOverlay != null) ImageOverlay.Children.Add(line);
                else MainCanvas.Children.Add(line);
                // Add X coordinate number at the top
                var text = new TextBlock
                {
                    Text = ((int)x).ToString(),
                    Foreground = Brushes.Gray,
                    FontSize = 10,
                    Tag = "GridNumber"
                };
                Canvas.SetLeft(text, x + 2);
                Canvas.SetTop(text, 2);
                if (ImageOverlay != null) ImageOverlay.Children.Add(text);
                else MainCanvas.Children.Add(text);
            }
            // Horizontal lines and numbers
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
                if (ImageOverlay != null) ImageOverlay.Children.Add(line);
                else MainCanvas.Children.Add(line);
                // Add Y coordinate number at the left
                var text = new TextBlock
                {
                    Text = ((int)y).ToString(),
                    Foreground = Brushes.Gray,
                    FontSize = 10,
                    Tag = "GridNumber"
                };
                Canvas.SetLeft(text, 2);
                Canvas.SetTop(text, y + 2);
                if (ImageOverlay != null) ImageOverlay.Children.Add(text);
                else MainCanvas.Children.Add(text);
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
                // If a vertex is selected, remove it; otherwise delete the whole room
                if (_selectedVertexIndex != null && Vm.SelectedRoom != null)
                {
                    var idx = _selectedVertexIndex.Value;
                    var room = Vm.SelectedRoom;
                    if (room.Points.Count > 3)
                    {
                        var removed = new Models.PointModel { X = room.Points[idx].X, Y = room.Points[idx].Y };
                        Vm.UndoRedo.Execute(
                            () => { room.Points.RemoveAt(idx); UpdateSelectionOverlay(Vm.SelectedRoom); _selectedVertexIndex = null; },
                            () => { room.Points.Insert(idx, removed); UpdateSelectionOverlay(Vm.SelectedRoom); }
                        );
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Polygon must have at least 3 points.", "Delete vertex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }
                else if (Vm.SelectedRoom != null)
                {
                    Vm.DeleteRoom(Vm.SelectedRoom);
                }
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
            // prefer point relative to the image control for image-coordinate operations
            System.Windows.Point p;
            if (BgImage != null)
            {
                if (e is MouseEventArgs me) p = me.GetPosition(BgImage);
                else if (e is MouseButtonEventArgs mbe) p = mbe.GetPosition(BgImage);
                else p = new System.Windows.Point(0, 0);
            }
            else
            {
                p = GetCanvasPoint(e);
            }
            // Convert UI/canvas point to image pixel coordinates
            if (BgImage.Source is System.Windows.Media.Imaging.BitmapSource bmp && BgImage.ActualWidth > 0 && BgImage.ActualHeight > 0)
            {
                var imgPxSize = new System.Windows.Size(bmp.PixelWidth, bmp.PixelHeight);
                var controlSize = new System.Windows.Size(BgImage.ActualWidth, BgImage.ActualHeight);
                var t = SVGMapper.Minimal.Services.ImageCoordinateTransformer.CalculateTransform(
                    imgPxSize, controlSize, bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0, bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0,
                    SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);
                // Invert the transform: UI point (relative to BgImage) -> image DIPs -> image pixels
                var xInImageDip = (p.X - t.OffsetX) / t.ScaleX;
                var yInImageDip = (p.Y - t.OffsetY) / t.ScaleY;
                var xPx = xInImageDip * (bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0);
                var yPx = yInImageDip * (bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0);
                // Clamp to image bounds
                xPx = Math.Max(0, Math.Min(imgPxSize.Width - 1, xPx));
                yPx = Math.Max(0, Math.Min(imgPxSize.Height - 1, yPx));

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

                    Vm.AddPolygonPoint(xPx, yPx);
                    // For preview, map image pixel back to UI (point is relative to BgImage)
                    var previewPt = SVGMapper.Minimal.Services.ImageCoordinateTransformer.TransformPoint(
                        new System.Windows.Point(xPx, yPx), imgPxSize, controlSize, bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0, bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0,
                        SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);
                    // Transform previewPt (which is relative to BgImage) to MainCanvas coordinates
                    if (BgImage != null)
                    {
                        var abs = BgImage.TranslatePoint(previewPt, MainCanvas);
                        _preview!.Points.Add(abs);
                    }
                    else
                    {
                        _preview!.Points.Add(previewPt);
                    }

                    if (_preview.Points.Count == 1)
                    {
                        var el = new Ellipse { Width = 12, Height = 12, Stroke = Brushes.DarkRed, StrokeThickness = 2, Fill = Brushes.White };
                        Vm.UndoRedo.Execute(
                            doAction: () =>
                            {
                                // place start marker at absolute MainCanvas coords
                                var markerPos = BgImage != null ? BgImage.TranslatePoint(previewPt, MainCanvas) : previewPt;
                                Canvas.SetLeft(el, markerPos.X - 6);
                                Canvas.SetTop(el, markerPos.Y - 6);
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
                    Vm.AddSeat(xPx, yPx);
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var p = GetCanvasPoint(e);
            if (_isDrawing && _preview != null)
            {
                // compute mouse position relative to BgImage and translate to MainCanvas absolute coords
                System.Windows.Point mouseUi;
                if (BgImage != null)
                {
                    if (e is MouseEventArgs me2) mouseUi = me2.GetPosition(BgImage);
                    else mouseUi = new System.Windows.Point(0, 0);
                    mouseUi = BgImage.TranslatePoint(mouseUi, MainCanvas);
                }
                else
                {
                    mouseUi = GetCanvasPoint(e);
                }

                if (_preview.Points.Count > 0)
                {
                    // leave existing, add temp point
                    if (_preview.Points.Count > Vm.CurrentDraftCount)
                    {
                        _preview.Points[_preview.Points.Count - 1] = mouseUi;
                    }
                    else
                    {
                        _preview.Points.Add(mouseUi);
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

        private void RoomsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RoomsList.SelectedItem is Models.PolygonRoom room)
            {
                var dlg = new InputDialog("Rename room:", room.Name ?? "Room");
                dlg.Owner = this;
                if (dlg.ShowDialog() == true)
                {
                    var newName = dlg.ResponseText?.Trim();
                    if (!string.IsNullOrEmpty(newName))
                    {
                        room.Name = newName;
                    }
                }
            }
        }

        private void RoomsList_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (RoomsList.SelectedItem is Models.PolygonRoom room)
            {
                Vm.DeleteRoom(room);
            }
        }

        private void RoomsList_Rename_Click(object sender, RoutedEventArgs e)
        {
            if (RoomsList.SelectedItem is Models.PolygonRoom room)
            {
                var dlg = new InputDialog("Rename room:", room.Name ?? "Room");
                dlg.Owner = this;
                if (dlg.ShowDialog() == true)
                {
                    var newName = dlg.ResponseText?.Trim();
                    if (!string.IsNullOrEmpty(newName))
                    {
                        room.Name = newName;
                    }
                }
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
                    IsHitTestVisible = true
                };
                overlayPath.MouseLeftButtonDown += SelectionOverlay_MouseLeftButtonDown;
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
                    // build vertex handles for editing
                    CreateVertexHandles(room);
                    _selectedVertexIndex = null;
                }
                return;
            }

            // Fallback: build polygon overlay from model points
            var poly = new Polygon
            {
                Stroke = Brushes.Orange,
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Color.FromArgb(60, 255, 165, 0)),
                IsHitTestVisible = true
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
            _selectionOverlay.MouseLeftButtonDown += SelectionOverlay_MouseLeftButtonDown;
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
                // build vertex handles for editing
                CreateVertexHandles(room);
                _selectedVertexIndex = null;
            }
        }

        private void CreateVertexHandles(Models.PolygonRoom room)
        {
            // remove previous handles
            foreach (var h in _vertexHandles) MainCanvas.Children.Remove(h);
            _vertexHandles.Clear();

            if (room == null) return;
            var bmp = BgImage?.Source as System.Windows.Media.Imaging.BitmapSource;
            EnsurePointsFromNormalized(room, bmp);
            if ((room.Points == null || room.Points.Count == 0) && (room.NormalizedPoints == null || room.NormalizedPoints.Count == 0)) return;
            if (bmp == null) return;

            var imgPxSize = new System.Windows.Size(bmp.PixelWidth, bmp.PixelHeight);
            var controlSize = new System.Windows.Size(BgImage.ActualWidth, BgImage.ActualHeight);
            var ptsForHandles = room.NormalizedPoints != null && room.NormalizedPoints.Count > 0
                ? room.NormalizedPoints.Select(n => new System.Windows.Point(n.X * imgPxSize.Width, n.Y * imgPxSize.Height)).ToList()
                : room.Points.Select(p => new System.Windows.Point(p.X, p.Y)).ToList();
            for (int i = 0; i < ptsForHandles.Count; i++)
            {
                var pt = ptsForHandles[i];
                var previewPt = SVGMapper.Minimal.Services.ImageCoordinateTransformer.TransformPoint(
                    new System.Windows.Point(pt.X, pt.Y), imgPxSize, controlSize, bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0, bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0,
                    SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);
                var abs = BgImage.TranslatePoint(previewPt, MainCanvas);
                var el = new Ellipse { Width = 10, Height = 10, Stroke = Brushes.Black, StrokeThickness = 1, Fill = (_selectedVertexIndex == i ? Brushes.Orange : Brushes.White), Tag = i };                Canvas.SetLeft(el, abs.X - 5);
                Canvas.SetTop(el, abs.Y - 5);
                Canvas.SetZIndex(el, 1000);
                el.MouseLeftButtonDown += Vertex_MouseLeftButtonDown;
                el.MouseMove += Vertex_MouseMove;
                el.MouseLeftButtonUp += Vertex_MouseLeftButtonUp;
                el.MouseRightButtonDown += Vertex_MouseRightButtonDown;
                MainCanvas.Children.Add(el);
                _vertexHandles.Add(el);
            }
        }

        private void Vertex_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse el && Vm.SelectedRoom != null && el.Tag is int idx)
            {
                // clear previous selection visual
                if (_selectedVertexIndex != null && _selectedVertexIndex >= 0 && _selectedVertexIndex < _vertexHandles.Count)
                {
                    _vertexHandles[_selectedVertexIndex.Value].Fill = Brushes.White;
                }
                _selectedVertexIndex = idx;
                _draggingVertexIndex = idx;
                _draggingRoom = Vm.SelectedRoom;
                _dragOriginalPoint = new System.Windows.Point(_draggingRoom.Points[idx].X, _draggingRoom.Points[idx].Y);
                el.CaptureMouse();
                el.Fill = Brushes.Orange;
                e.Handled = true;
            }
        }

        private void Vertex_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_draggingVertexIndex == null || _draggingRoom == null) return;
            if (!(sender is Ellipse el)) return;
            if (!el.IsMouseCaptured) return;
            if (e is MouseEventArgs me && BgImage?.Source is System.Windows.Media.Imaging.BitmapSource bmp)
            {
                var mouseUi = me.GetPosition(BgImage);
                // compute image pixel coords same as in Canvas_MouseLeftButtonDown
                var imgPxSize = new System.Windows.Size(bmp.PixelWidth, bmp.PixelHeight);
                var controlSize = new System.Windows.Size(BgImage.ActualWidth, BgImage.ActualHeight);
                var t = SVGMapper.Minimal.Services.ImageCoordinateTransformer.CalculateTransform(
                    imgPxSize, controlSize, bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0, bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0,
                    SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);
                var xInImageDip = (mouseUi.X - t.OffsetX) / t.ScaleX;
                var yInImageDip = (mouseUi.Y - t.OffsetY) / t.ScaleY;
                var xPx = xInImageDip * (bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0);
                var yPx = yInImageDip * (bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0);
                xPx = Math.Max(0, Math.Min(imgPxSize.Width - 1, xPx));
                yPx = Math.Max(0, Math.Min(imgPxSize.Height - 1, yPx));

                // snap to grid
                var snapped = Vm.Snap(new System.Windows.Point(xPx, yPx));
                var idx = _draggingVertexIndex.Value;
                _draggingRoom.Points[idx].X = snapped.X;
                _draggingRoom.Points[idx].Y = snapped.Y;

                // update overlay visuals
                // update selection overlay if present
                if (_selectionOverlay != null && _selectionOverlay.Points.Count > idx)
                {
                    _selectionOverlay.Points[idx] = new System.Windows.Point(snapped.X, snapped.Y);
                }

                // reposition handle
                var previewPt = SVGMapper.Minimal.Services.ImageCoordinateTransformer.TransformPoint(
                    new System.Windows.Point(snapped.X, snapped.Y), imgPxSize, controlSize, bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0, bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0,
                    SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);
                var abs = BgImage.TranslatePoint(previewPt, MainCanvas);
                Canvas.SetLeft(el, abs.X - el.Width / 2);
                Canvas.SetTop(el, abs.Y - el.Height / 2);

                // redraw polygons overlay (image overlay) so main view reflects change
                DrawPolygonsOverlay();
            }
        }

        private void Vertex_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            if (!(sender is Ellipse el)) return;
            if (_draggingRoom == null || _draggingVertexIndex == null || _dragOriginalPoint == null) return;
            var idx = _draggingVertexIndex.Value;
            var newPoint = new System.Windows.Point(_draggingRoom.Points[idx].X, _draggingRoom.Points[idx].Y);
            var oldPoint = _dragOriginalPoint.Value;

            // commit undo entry
            Vm.UndoRedo.Execute(
                doAction: () => { _draggingRoom.Points[idx].X = newPoint.X; _draggingRoom.Points[idx].Y = newPoint.Y; DrawPolygonsOverlay(); },
                undoAction: () => { _draggingRoom.Points[idx].X = oldPoint.X; _draggingRoom.Points[idx].Y = oldPoint.Y; DrawPolygonsOverlay(); }
            );

            el.ReleaseMouseCapture();
            el.Fill = Brushes.White;
            _draggingVertexIndex = null;
            _dragOriginalPoint = null;
            _draggingRoom = null;
            e.Handled = true;
        }

        private void Vertex_MouseRightButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse el && Vm.SelectedRoom != null && el.Tag is int idx)
            {
                var room = Vm.SelectedRoom;
                if (room.Points.Count <= 3)
                {
                    System.Windows.MessageBox.Show("Polygon must have at least 3 points.", "Remove vertex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }
                var removed = new Models.PointModel { X = room.Points[idx].X, Y = room.Points[idx].Y };
                Vm.UndoRedo.Execute(
                    () => { room.Points.RemoveAt(idx); UpdateSelectionOverlay(Vm.SelectedRoom); },
                    () => { room.Points.Insert(idx, removed); UpdateSelectionOverlay(Vm.SelectedRoom); }
                );
                e.Handled = true;
            }
        }

        private void SelectionOverlay_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // double-click an edge to insert a vertex
            if (e.ClickCount >= 2 && Vm.SelectedRoom != null && BgImage?.Source is System.Windows.Media.Imaging.BitmapSource bmp)
            {
                var mouseUi = e.GetPosition(BgImage);
                var imgPxSize = new System.Windows.Size(bmp.PixelWidth, bmp.PixelHeight);
                var controlSize = new System.Windows.Size(BgImage.ActualWidth, BgImage.ActualHeight);
                var t = SVGMapper.Minimal.Services.ImageCoordinateTransformer.CalculateTransform(
                    imgPxSize, controlSize, bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0, bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0,
                    SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);
                var xInImageDip = (mouseUi.X - t.OffsetX) / t.ScaleX;
                var yInImageDip = (mouseUi.Y - t.OffsetY) / t.ScaleY;
                var xPx = xInImageDip * (bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0);
                var yPx = yInImageDip * (bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0);
                xPx = Math.Max(0, Math.Min(imgPxSize.Width - 1, xPx));
                yPx = Math.Max(0, Math.Min(imgPxSize.Height - 1, yPx));

                // find nearest segment
                var room = Vm.SelectedRoom;
                EnsurePointsFromNormalized(room, bmp);
                int bestIdx = -1; double bestDist = double.MaxValue;
                for (int i = 0; i < room.Points.Count; i++)
                {
                    var a = room.Points[i];
                    var b = room.Points[(i + 1) % room.Points.Count];
                    var dist = DistancePointToSegment(xPx, yPx, a.X, a.Y, b.X, b.Y);
                    if (dist < bestDist) { bestDist = dist; bestIdx = i; }
                }

                if (bestIdx >= 0)
                {
                    var insertIdx = bestIdx + 1;
                    var newPt = new Models.PointModel { X = xPx, Y = yPx };
                    Vm.UndoRedo.Execute(
                        () => { room.Points.Insert(insertIdx, newPt); UpdateSelectionOverlay(Vm.SelectedRoom); },
                        () => { room.Points.RemoveAt(insertIdx); UpdateSelectionOverlay(Vm.SelectedRoom); }
                    );
                }
                e.Handled = true;
            }
        }

        private static double DistancePointToSegment(double px, double py, double x1, double y1, double x2, double y2)
        {
            // from https://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
            double dx = x2 - x1; double dy = y2 - y1;
            if (dx == 0 && dy == 0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            double t = ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            double projx = x1 + t * dx; double projy = y1 + t * dy;
            var ddx = px - projx; var ddy = py - projy;
            return Math.Sqrt(ddx * ddx + ddy * ddy);
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