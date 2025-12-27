using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SVGMapper.Models;
using SVGMapper.Services;

namespace SVGMapper.Views
{
    public partial class FloorPlanView : UserControl
    {
        private readonly List<Room> _rooms = new();
        private readonly Dictionary<Room, Polygon> _roomElements = new();
        private readonly Dictionary<Room, TextBlock> _roomLabels = new();
        private readonly Dictionary<Room, List<Ellipse>> _vertexHandles = new();

        private bool _isDrawing = false;
        private List<Point> _currentPoints = new();
        private Polyline? _previewLine;

        // Vertex dragging
        private bool _isDraggingVertex = false;
        private Room? _dragRoom;
        private int _dragVertexIndex = -1;
        private Point _dragStartPoint;
        private List<Point>? _dragOriginalPoints;

        public SelectionService? SelectionService { get; set; }
        public UndoService? UndoService { get; set; }

        private int _nextRoomNumber = 1;

        public FloorPlanView()
        {
            InitializeComponent();
            ZoomCanvas.Canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            ZoomCanvas.Canvas.MouseMove += Canvas_MouseMove;
            ZoomCanvas.Canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp_ForVertex;

            Loaded += (s, e) => { ZoomCanvas.Canvas.Focus(); };
            ZoomCanvas.Canvas.KeyDown += Canvas_KeyDown;
        }

        private void Canvas_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var moveAmount = (e.KeyboardDevice.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0 ? 10 : 1;

            if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelActiveTool();
                e.Handled = true;
                return;
            }

            if (e.Key == System.Windows.Input.Key.Left) { NudgeSelectedRooms(-moveAmount, 0); e.Handled = true; }
            else if (e.Key == System.Windows.Input.Key.Right) { NudgeSelectedRooms(moveAmount, 0); e.Handled = true; }
            else if (e.Key == System.Windows.Input.Key.Up) { NudgeSelectedRooms(0, -moveAmount); e.Handled = true; }
            else if (e.Key == System.Windows.Input.Key.Down) { NudgeSelectedRooms(0, moveAmount); e.Handled = true; }
        }

        private void NudgeSelectedRooms(double dx, double dy)
        {
            var selectedRooms = new List<Room>();
            foreach (var obj in SelectionService?.SelectedItems ?? System.Linq.Enumerable.Empty<object>()) if (obj is Room r) selectedRooms.Add(r);
            if (selectedRooms.Count == 0) return;

            foreach (var room in selectedRooms)
            {
                var from = new List<System.Windows.Point>(room.Points);
                var to = new List<System.Windows.Point>();
                foreach (var p in room.Points) to.Add(new System.Windows.Point(p.X + dx, p.Y + dy));

                room.SetPoints(to);

                if (UndoService != null)
                {
                    var action = new MoveRoomVerticesAction(room, from, to);
                    UndoService.Do(action);
                }
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = ZoomCanvas.ScreenToWorldPoint(e.GetPosition(ZoomCanvas));
            pos = ZoomCanvas.SnapToGridPoint(pos);

            if (PolygonToggle.IsChecked == true)
            {
                if (!_isDrawing)
                {
                    _isDrawing = true;
                    _currentPoints.Clear();

                    _previewLine = new Polyline
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    };
                    ZoomCanvas.Canvas.Children.Add(_previewLine);
                }

                _currentPoints.Add(pos);
                _previewLine!.Points.Add(pos);
                e.Handled = true;
                return;
            }

            // clicking on room fills selection if clicked on polygon
            if (e.OriginalSource is Polygon poly && poly.Tag is Room room)
            {
                SelectionService?.Select(room);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && _previewLine != null)
            {
                var pos = ZoomCanvas.ScreenToWorldPoint(e.GetPosition(ZoomCanvas));
                pos = ZoomCanvas.SnapToGridPoint(pos);
                if (_previewLine.Points.Count > 0)
                {
                    // temporary last point as mouse
                    if (_previewLine.Points.Count > _currentPoints.Count)
                        _previewLine.Points[_previewLine.Points.Count - 1] = pos;
                    else
                        _previewLine.Points.Add(pos);
                }
            }

            if (_isDraggingVertex && _dragRoom != null && _dragOriginalPoints != null)
            {
                var p = ZoomCanvas.ScreenToWorldPoint(e.GetPosition(ZoomCanvas));
                p = ZoomCanvas.SnapToGridPoint(p);

                // Update the dragged vertex position
                var pts = new List<Point>(_dragOriginalPoints);
                pts[_dragVertexIndex] = p;
                _dragRoom.SetPoints(pts);

                // update handle position
                if (_vertexHandles.TryGetValue(_dragRoom, out var handles) && _dragVertexIndex >= 0 && _dragVertexIndex < handles.Count)
                {
                    var el = handles[_dragVertexIndex];
                    Canvas.SetLeft(el, p.X - el.Width / 2);
                    Canvas.SetTop(el, p.Y - el.Height / 2);
                }
            }
        }

        private void ClosePolygon_Click(object sender, RoutedEventArgs e)
        {
            FinishPolygon();
        }

        private void Canvas_MouseLeftButtonUp_ForVertex(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingVertex && _dragRoom != null && _dragOriginalPoints != null)
            {
                var finalPoints = new List<Point>(_dragRoom.Points);
                // create undo action if something changed
                bool changed = false;
                if (finalPoints.Count == _dragOriginalPoints.Count)
                {
                    for (int i = 0; i < finalPoints.Count; i++) if (finalPoints[i] != _dragOriginalPoints[i]) { changed = true; break; }
                }

                if (changed && UndoService != null)
                {
                    var action = new MoveRoomVerticesAction(_dragRoom, new List<Point>(_dragOriginalPoints), finalPoints);
                    UndoService.Do(action);
                }

                _isDraggingVertex = false;
                _dragRoom = null;
                _dragVertexIndex = -1;
                _dragOriginalPoints = null;
                ZoomCanvas.Canvas.ReleaseMouseCapture();
            }
        }
        private void FinishPolygon()
        {
            if (!_isDrawing || _currentPoints.Count < 3) return;

            // Validate polygon (no self-intersections)
            if (IsSelfIntersecting(_currentPoints))
            {
                MessageBox.Show("The polygon you drew has self-intersections. Please adjust the vertices.", "Invalid polygon", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prompt for room name
            var defaultName = "Room " + _nextRoomNumber;
            var chosen = PromptForRoomName(defaultName);
            if (string.IsNullOrEmpty(chosen)) return; // user cancelled
            _nextRoomNumber++;

            // create room model
            var room = new Room { Name = chosen, Points = new List<Point>(_currentPoints) };

            if (UndoService != null)
            {
                var action = new AddRoomAction(this, room);
                UndoService.Do(action);
            }
            else
            {
                AddRoomInternal(room);
            }

            // cleanup
            if (_previewLine != null) ZoomCanvas.Canvas.Children.Remove(_previewLine);
            _previewLine = null;
            _currentPoints.Clear();
            _isDrawing = false;
        }

        public void AddRoomInternal(Room room)
        {
            _rooms.Add(room);

            var poly = new Polygon
            {
                Points = new PointCollection(room.Points),
                Stroke = new SolidColorBrush(room.StrokeColor),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(room.FillColor),
                Opacity = room.Opacity,
                Tag = room
            };

            poly.MouseLeftButtonDown += Poly_MouseLeftButtonDown;
            ZoomCanvas.Canvas.Children.Add(poly);
            _roomElements[room] = poly;

            // label
            var center = room.Center;
            var txt = new TextBlock { Text = room.Name, Foreground = Brushes.Black };
            Canvas.SetLeft(txt, center.X);
            Canvas.SetTop(txt, center.Y);
            // adjust after measure to truly center
            txt.Loaded += (s, e) => { Canvas.SetLeft(txt, room.Center.X - (txt.ActualWidth / 2)); Canvas.SetTop(txt, room.Center.Y - (txt.ActualHeight / 2)); };
            ZoomCanvas.Canvas.Children.Add(txt);
            _roomLabels[room] = txt;

            room.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectableBase.IsSelected)) UpdateRoomVisual(room);
                if (e.PropertyName == nameof(Room.Points))
                {
                    // update polygon and label
                    if (_roomElements.TryGetValue(room, out var p)) p.Points = new PointCollection(room.Points);
                    if (_roomLabels.TryGetValue(room, out var t))
                    {
                        var c = room.Center;
                        // center text block around room center
                        Canvas.SetLeft(t, c.X - (t.ActualWidth / 2));
                        Canvas.SetTop(t, c.Y - (t.ActualHeight / 2));
                    }
                }
                if (e.PropertyName == nameof(Room.StrokeColor) || e.PropertyName == nameof(Room.FillColor) || e.PropertyName == nameof(Room.Opacity))
                {
                    if (_roomElements.TryGetValue(room, out var p))
                    {
                        p.Stroke = new SolidColorBrush(room.StrokeColor);
                        p.Fill = new SolidColorBrush(room.FillColor) { Opacity = room.Opacity };
                    }
                }
            };

            UpdateRoomVisual(room);

        }

        public void RemoveRoomInternal(Room room)
        {
            if (_roomElements.TryGetValue(room, out var poly))
            {
                ZoomCanvas.Canvas.Children.Remove(poly);
                _roomElements.Remove(room);
            }
            if (_roomLabels.TryGetValue(room, out var label))
            {
                ZoomCanvas.Canvas.Children.Remove(label);
                _roomLabels.Remove(room);
            }
            RemoveVertexHandles(room);
            _rooms.Remove(room);
        }

        public IEnumerable<Room> GetRooms() => _rooms;

        /// <summary>
        /// Cancels active polygon drawing or vertex dragging and other in-progress actions.
        /// </summary>
        public void CancelActiveTool()
        {
            // Cancel polygon drawing
            if (_isDrawing)
            {
                _isDrawing = false;
                _currentPoints.Clear();
                if (_previewLine != null) { ZoomCanvas.Canvas.Children.Remove(_previewLine); _previewLine = null; }
            }

            // Cancel vertex dragging
            if (_isDraggingVertex && _dragRoom != null && _dragOriginalPoints != null)
            {
                _dragRoom.SetPoints(new List<System.Windows.Point>(_dragOriginalPoints));
                _isDraggingVertex = false;
                _dragRoom = null;
                _dragVertexIndex = -1;
                _dragOriginalPoints = null;
                ZoomCanvas.Canvas.ReleaseMouseCapture();
            }
        }

        public void DeleteSelection()
        {
            var roomsToDelete = new List<Room>();
            foreach (var obj in SelectionService?.SelectedItems ?? System.Linq.Enumerable.Empty<object>()) if (obj is Room r) roomsToDelete.Add(r);

            if (roomsToDelete.Count == 0) return;

            if (UndoService != null)
            {
                UndoService.Do(new DeleteRoomsAction(this, roomsToDelete));
            }
            else
            {
                foreach (var r in roomsToDelete) RemoveRoomInternal(r);
            }

            SelectionService?.Clear();
        }

        private void Poly_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is Polygon poly && poly.Tag is Room room)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    SelectionService?.Toggle(room);
                else
                    SelectionService?.Select(room);

                // If the room is selected, create vertex handles so user can drag vertices
                if (room.IsSelected)
                    ShowVertexHandles(room);

                e.Handled = true;
            }
        }

        private void ShowVertexHandles(Room room)
        {
            // remove existing handles for other rooms
            foreach (var kv in _vertexHandles)
            {
                if (kv.Key != room)
                {
                    foreach (var h in kv.Value) ZoomCanvas.Canvas.Children.Remove(h);
                }
            }

            if (_vertexHandles.ContainsKey(room)) return; // already shown

            var list = new List<Ellipse>();
            for (int i = 0; i < room.Points.Count; i++)
            {
                var pt = room.Points[i];
                var el = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Tag = (room, i)
                };
                Canvas.SetLeft(el, pt.X - el.Width / 2);
                Canvas.SetTop(el, pt.Y - el.Height / 2);
                el.MouseLeftButtonDown += Vertex_MouseLeftButtonDown;
                ZoomCanvas.Canvas.Children.Add(el);
                list.Add(el);
            }

            _vertexHandles[room] = list;
        }

        private void RemoveVertexHandles(Room room)
        {
            if (!_vertexHandles.TryGetValue(room, out var list)) return;
            foreach (var el in list) ZoomCanvas.Canvas.Children.Remove(el);
            _vertexHandles.Remove(room);
        }

        private void Vertex_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse el && el.Tag is ValueTuple<Room, int> t)
            {
                _isDraggingVertex = true;
                _dragRoom = t.Item1;
                _dragVertexIndex = t.Item2;
                _dragStartPoint = ZoomCanvas.ScreenToWorldPoint(e.GetPosition(ZoomCanvas));
                _dragOriginalPoints = new List<Point>(_dragRoom.Points);

                ZoomCanvas.Canvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void UpdateRoomVisual(Room room)
        {
            if (!_roomElements.TryGetValue(room, out var poly)) return;
            if (room.IsSelected)
            {
                poly.Stroke = Brushes.Gold;
                poly.StrokeThickness = 3;

                // show vertex handles
                ShowVertexHandles(room);
            }
            else
            {
                poly.Stroke = new SolidColorBrush(room.StrokeColor);
                poly.StrokeThickness = 2;

                // remove vertex handles
                RemoveVertexHandles(room);
            }
        }

        private bool IsSelfIntersecting(List<Point> pts)
        {
            // Check each pair of non-adjacent segments for intersection
            bool SegIntersects(Point a1, Point a2, Point b1, Point b2)
            {
                double Cross(Point p, Point q) => p.X * q.Y - p.Y * q.X;
                Point Sub(Point p, Point q) => new Point(p.X - q.X, p.Y - q.Y);

                var r = Sub(a2, a1);
                var s = Sub(b2, b1);
                var denom = Cross(r, s);
                if (Math.Abs(denom) < 1e-8) return false; // parallel

                var u = Cross(Sub(b1, a1), r) / denom;
                var t = Cross(Sub(b1, a1), s) / denom;

                return t > 0 && t < 1 && u > 0 && u < 1;
            }

            var n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                var a1 = pts[i];
                var a2 = pts[(i + 1) % n];
                for (int j = i + 1; j < n; j++)
                {
                    // skip adjacent segments
                    if (j == i || j == i + 1 || (i == 0 && j == n - 1)) continue;
                    var b1 = pts[j];
                    var b2 = pts[(j + 1) % n];
                    if (SegIntersects(a1, a2, b1, b2)) return true;
                }
            }
            return false;
        }

        private string? PromptForRoomName(string defaultName)
        {
            var win = new Window
            {
                Title = "Room name",
                Width = 360,
                Height = 140,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var sp = new StackPanel { Margin = new Thickness(10) };
            var label = new TextBlock { Text = "Enter room name:", Margin = new Thickness(0, 0, 0, 6) };
            var tb = new TextBox { Text = defaultName };

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var ok = new Button { Content = "OK", Width = 80, IsDefault = true };
            var cancel = new Button { Content = "Cancel", Width = 80, IsCancel = true, Margin = new Thickness(6, 0, 0, 0) };
            ok.Click += (s, e) => { win.DialogResult = true; win.Close(); };
            cancel.Click += (s, e) => { win.DialogResult = false; win.Close(); };
            btnPanel.Children.Add(ok); btnPanel.Children.Add(cancel);

            sp.Children.Add(label);
            sp.Children.Add(tb);
            sp.Children.Add(btnPanel);

            win.Content = sp;

            var res = win.ShowDialog();
            return res == true ? tb.Text : null;
        }

        private void ClearRooms_Click(object sender, RoutedEventArgs e)
        {
            foreach (var kv in _roomElements) ZoomCanvas.Canvas.Children.Remove(kv.Value);
            _roomElements.Clear();
            _rooms.Clear();
            SelectionService?.Clear();
        }
    }
}