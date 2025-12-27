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
    public partial class SeatingPlanView : UserControl
    {
        private readonly Dictionary<Seat, Ellipse> _seatElements = new();
        private readonly Dictionary<Seat, TextBlock> _seatLabels = new();
        private int _nextSeatNumber = 1;
        private char _nextRowLetter = 'A';

        private bool _isSelecting = false;
        private Point _selectionStartWorld;
        private Rectangle? _rubberRect;

        // Row tool
        private bool _isRowPlacing = false;
        private Point _rowStartWorld;
        private Line? _rowPreviewLine;
        private readonly List<Ellipse> _rowPreviewSeats = new();

        public SelectionService? SelectionService { get; set; }
        public UndoService? UndoService { get; set; }

        // Dragging state
        private bool _isDraggingSeats = false;
        private Point _dragStartWorld;
        private Dictionary<Seat, Point>? _dragOriginalPositions;

        public SeatingPlanView()
        {
            InitializeComponent();

            ZoomCanvas.Canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            ZoomCanvas.Canvas.MouseMove += Canvas_MouseMove;
            ZoomCanvas.Canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;

            // keyboard events for nudging and shortcuts
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

            if (e.Key == System.Windows.Input.Key.Delete)
            {
                DeleteSelection();
                e.Handled = true;
                return;
            }

            if (e.Key == System.Windows.Input.Key.Left)
            {
                NudgeSelection(-moveAmount, 0);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Right)
            {
                NudgeSelection(moveAmount, 0);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                NudgeSelection(0, -moveAmount);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                NudgeSelection(0, moveAmount);
                e.Handled = true;
            }
        }

        private void NudgeSelection(double dx, double dy)
        {
            var selectedSeats = new List<Seat>();
            foreach (var obj in SelectionService?.SelectedItems ?? System.Linq.Enumerable.Empty<object>()) if (obj is Seat s) selectedSeats.Add(s);
            if (selectedSeats.Count == 0) return;

            var changes = new List<(Seat seat, System.Windows.Point from, System.Windows.Point to)>();
            foreach (var seat in selectedSeats)
            {
                var from = seat.Position;
                var to = new System.Windows.Point(from.X + dx, from.Y + dy);
                seat.Position = to; // will update visuals via binding
                changes.Add((seat, from, to));
            }

            if (UndoService != null && changes.Count > 0)
                UndoService.Do(new MoveSeatsAction(changes));
        }
        private void AddSeatToggle_Click(object sender, RoutedEventArgs e)
        {
            // Toggle handled by ToggleButton state - no extra logic here
        }

        private void ClearSeats_Click(object sender, RoutedEventArgs e)
        {
            foreach (var el in _seatElements.Values)
                ZoomCanvas.Canvas.Children.Remove(el);
            _seatElements.Clear();
            _nextSeatNumber = 1;
            SelectionService?.Clear();
        }

        public IEnumerable<Seat> GetSeats() => _seatElements.Keys;

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var posScreen = e.GetPosition(ZoomCanvas);
            var posWorld = ZoomCanvas.ScreenToWorldPoint(posScreen);
            posWorld = ZoomCanvas.SnapToGridPoint(posWorld);

            // If in row tool mode: start/finish placing a row
            if (RowToolToggle.IsChecked == true)
            {
                if (!_isRowPlacing)
                {
                    _isRowPlacing = true;
                    _rowStartWorld = posWorld;

                    // preview line
                    _rowPreviewLine = new Line
                    {
                        Stroke = Brushes.OrangeRed,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    };
                    _rowPreviewLine.X1 = posWorld.X;
                    _rowPreviewLine.Y1 = posWorld.Y;
                    _rowPreviewLine.X2 = posWorld.X;
                    _rowPreviewLine.Y2 = posWorld.Y;
                    ZoomCanvas.Canvas.Children.Add(_rowPreviewLine);

                    ZoomCanvas.Canvas.CaptureMouse();
                }
                else
                {
                    // Finish row placement
                    _isRowPlacing = false;
                    ZoomCanvas.Canvas.ReleaseMouseCapture();

                    var spacing = 40.0;
                    if (!double.TryParse(RowSpacingBox.Text, out spacing) || spacing <= 0) spacing = 40.0;

                    var endPoint = ZoomCanvas.SnapToGridPoint(posWorld);
                    var seats = GenerateSeatsAlongLine(_rowStartWorld, endPoint, spacing);

                    if (seats.Count > 0)
                    {
                        // assign row name and seat numbers
                        var rowName = _nextRowLetter.ToString();
                        for (int i = 0; i < seats.Count; i++)
                        {
                            seats[i].SeatNumber = rowName + (i + 1).ToString();
                            seats[i].Row = rowName;
                            seats[i].Color = System.Windows.Media.Colors.SteelBlue;
                            seats[i].Size = 20;
                        }

                        // advance row letter
                        if (_nextRowLetter == 'Z') _nextRowLetter = 'A'; else _nextRowLetter++;

                        var action = new AddSeatsAction(this, seats);
                        if (UndoService != null)
                            UndoService.Do(action);
                        else
                            action.Execute(); // fallback when UndoService not wired
                    }

                    // cleanup preview
                    if (_rowPreviewLine != null) ZoomCanvas.Canvas.Children.Remove(_rowPreviewLine);
                    _rowPreviewLine = null;
                    foreach (var p in _rowPreviewSeats) ZoomCanvas.Canvas.Children.Remove(p);
                    _rowPreviewSeats.Clear();
                }

                e.Handled = true;
                return;
            }

            // If in add-seat mode and click on the canvas -> add seat
            if (AddSeatToggle.IsChecked == true)
            {
                CreateSeatAt(posWorld);
                e.Handled = true;
                return;
            }

            // If clicked background (not on a seat), start selection rectangle
            if (e.OriginalSource is Canvas)
            {
                _isSelecting = true;
                _selectionStartWorld = posWorld;

                _rubberRect = new Rectangle
                {
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    Fill = new SolidColorBrush(Color.FromArgb(32, 30, 144, 255))
                };

                Canvas.SetLeft(_rubberRect, _selectionStartWorld.X);
                Canvas.SetTop(_rubberRect, _selectionStartWorld.Y);
                _rubberRect.Width = 0;
                _rubberRect.Height = 0;
                ZoomCanvas.Canvas.Children.Add(_rubberRect);

                ZoomCanvas.Canvas.CaptureMouse();
                e.Handled = true;
            }            
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting && _rubberRect != null)
            {
                var p = e.GetPosition(ZoomCanvas);
                var world = ZoomCanvas.ScreenToWorldPoint(p);

                var x = Math.Min(_selectionStartWorld.X, world.X);
                var y = Math.Min(_selectionStartWorld.Y, world.Y);
                var w = Math.Abs(world.X - _selectionStartWorld.X);
                var h = Math.Abs(world.Y - _selectionStartWorld.Y);

                Canvas.SetLeft(_rubberRect, x);
                Canvas.SetTop(_rubberRect, y);
                _rubberRect.Width = w;
                _rubberRect.Height = h;
            }

            // Row preview while placing
            if (_isRowPlacing && _rowPreviewLine != null)
            {
                var p = e.GetPosition(ZoomCanvas);
                var world = ZoomCanvas.ScreenToWorldPoint(p);
                world = ZoomCanvas.SnapToGridPoint(world);

                _rowPreviewLine.X2 = world.X;
                _rowPreviewLine.Y2 = world.Y;

                // update preview seats
                foreach (var pr in _rowPreviewSeats) ZoomCanvas.Canvas.Children.Remove(pr);
                _rowPreviewSeats.Clear();

                var spacing = 40.0;
                if (!double.TryParse(RowSpacingBox.Text, out spacing) || spacing <= 0) spacing = 40.0;

                var previewSeats = GenerateSeatPositions(_rowStartWorld, world, spacing);
                foreach (var pos in previewSeats)
                {
                    var displayPos = ZoomCanvas.SnapToGridPoint(pos);
                    var el = new Ellipse
                    {
                        Width = 16,
                        Height = 16,
                        Fill = new SolidColorBrush(Color.FromArgb(160, 30, 144, 255)),
                        Stroke = Brushes.Transparent
                    };
                    Canvas.SetLeft(el, displayPos.X - el.Width / 2);
                    Canvas.SetTop(el, displayPos.Y - el.Height / 2);
                    ZoomCanvas.Canvas.Children.Add(el);
                    _rowPreviewSeats.Add(el);
                }
            }

            // Handle dragging selected seats
            if (_isDraggingSeats && _dragOriginalPositions != null)
            {
                var p = e.GetPosition(ZoomCanvas);
                var world = ZoomCanvas.ScreenToWorldPoint(p);
                var dx = world.X - _dragStartWorld.X;
                var dy = world.Y - _dragStartWorld.Y;

                foreach (var kv in _dragOriginalPositions)
                {
                    var seat = kv.Key;
                    var orig = kv.Value;
                    var newPos = new Point(orig.X + dx, orig.Y + dy);

                    // Snap if enabled
                    if (ZoomCanvas.SnapToGrid && ZoomCanvas.GridSize > 0)
                    {
                        newPos = ZoomCanvas.SnapToGridPoint(newPos);
                    }

                    seat.Position = newPos;
                    if (_seatElements.TryGetValue(seat, out var el))
                    {
                        Canvas.SetLeft(el, newPos.X - el.Width / 2);
                        Canvas.SetTop(el, newPos.Y - el.Height / 2);
                    }
                }
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                ZoomCanvas.Canvas.ReleaseMouseCapture();
                _isSelecting = false;

                if (_rubberRect != null)
                {
                    var rectX = Canvas.GetLeft(_rubberRect);
                    var rectY = Canvas.GetTop(_rubberRect);
                    var rectW = _rubberRect.Width;
                    var rectH = _rubberRect.Height;

                    var minX = rectX;
                    var maxX = rectX + rectW;
                    var minY = rectY;
                    var maxY = rectY + rectH;

                    // Select seats whose center lies within the rectangle
                    SelectionService?.Clear();
                    foreach (var kv in _seatElements)
                    {
                        var seat = kv.Key;
                        var p = seat.Position;
                        if (p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY)
                            SelectionService?.Add(seat);
                    }

                    ZoomCanvas.Canvas.Children.Remove(_rubberRect);
                    _rubberRect = null;
                }
            }

            // Finish placing a row if currently placing (shouldn't usually happen on mouseup, but to be safe)
            if (_isRowPlacing && _rowPreviewLine != null)
            {
                _isRowPlacing = false;
                ZoomCanvas.Canvas.ReleaseMouseCapture();
                if (_rowPreviewLine != null) ZoomCanvas.Canvas.Children.Remove(_rowPreviewLine);
                _rowPreviewLine = null;
                foreach (var p in _rowPreviewSeats) ZoomCanvas.Canvas.Children.Remove(p);
                _rowPreviewSeats.Clear();
            }

            // Finish dragging seats
            if (_isDraggingSeats && _dragOriginalPositions != null)
            {
                var changes = new List<(Seat seat, Point from, Point to)>();
                foreach (var kv in _dragOriginalPositions)
                {
                    var seat = kv.Key;
                    var from = kv.Value;
                    var to = seat.Position;
                    if (from != to) changes.Add((seat, from, to));
                }

                if (changes.Count > 0 && UndoService != null)
                {
                    var action = new MoveSeatsAction(changes);
                    UndoService.Do(action);
                }

                _isDraggingSeats = false;
                _dragOriginalPositions = null;
            }
        }

        // Internal add/remove helpers used by undo actions
        public void AddSeatInternal(Seat seat)
        {
            // Ensure seat number is present
            if (string.IsNullOrEmpty(seat.SeatNumber)) seat.SeatNumber = (_nextSeatNumber++).ToString();

            var el = new Ellipse
            {
                Width = seat.Size,
                Height = seat.Size,
                Fill = new SolidColorBrush(seat.Color),
                Stroke = Brushes.Transparent,
                StrokeThickness = 2,
                Tag = seat
            };

            Canvas.SetLeft(el, seat.Position.X - el.Width / 2);
            Canvas.SetTop(el, seat.Position.Y - el.Height / 2);

            el.MouseLeftButtonDown += El_MouseLeftButtonDown;

            // add a visible label for the seat number
            var label = new TextBlock
            {
                Text = seat.SeatNumber,
                FontSize = 12,
                Foreground = Brushes.Black,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(label, seat.Position.X - (label.ActualWidth / 2));
            Canvas.SetTop(label, seat.Position.Y + (el.Height / 2) + 4);

            ZoomCanvas.Canvas.Children.Add(el);
            ZoomCanvas.Canvas.Children.Add(label);

            _seatElements[seat] = el;
            _seatLabels[seat] = label;

            seat.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectableBase.IsSelected))
                    UpdateSelectionVisual(seat);
                if (e.PropertyName == nameof(Seat.Position))
                {
                    // update UI position
                    double seatHeight = 20;
                    if (_seatElements.TryGetValue(seat, out var ellipse))
                    {
                        Canvas.SetLeft(ellipse, seat.Position.X - ellipse.Width / 2);
                        Canvas.SetTop(ellipse, seat.Position.Y - ellipse.Height / 2);
                        seatHeight = ellipse.Height;
                    }
                    if (_seatLabels.TryGetValue(seat, out var lbl))
                    {
                        Canvas.SetLeft(lbl, seat.Position.X - (lbl.ActualWidth / 2));
                        Canvas.SetTop(lbl, seat.Position.Y + (seatHeight / 2) + 4);
                    }
                }
                if (e.PropertyName == nameof(Seat.SeatNumber))
                {
                    if (_seatLabels.TryGetValue(seat, out var lbl)) lbl.Text = seat.SeatNumber;
                }
                if (e.PropertyName == nameof(Seat.Color) || e.PropertyName == nameof(Seat.Size))
                {
                    if (_seatElements.TryGetValue(seat, out var ellipse))
                    {
                        ellipse.Fill = new SolidColorBrush(seat.Color);
                        ellipse.Width = seat.Size;
                        ellipse.Height = seat.Size;

                        // Update label position to stay below seat
                        if (_seatLabels.TryGetValue(seat, out var lbl))
                        {
                            Canvas.SetLeft(lbl, seat.Position.X - (lbl.ActualWidth / 2));
                            Canvas.SetTop(lbl, seat.Position.Y + (ellipse.Height / 2) + 4);
                        }
                    }
                }
            };

            UpdateSelectionVisual(seat);
        }

        public void RemoveSeatInternal(Seat seat)
        {
            if (_seatElements.TryGetValue(seat, out var el))
            {
                ZoomCanvas.Canvas.Children.Remove(el);
                _seatElements.Remove(seat);
            }
            if (_seatLabels.TryGetValue(seat, out var lbl))
            {
                ZoomCanvas.Canvas.Children.Remove(lbl);
                _seatLabels.Remove(seat);
            }
        }

        private void CreateSeatAt(Point world)
        {
            var seat = new Seat { SeatNumber = "S" + (_nextSeatNumber++).ToString(), Position = world, Color = System.Windows.Media.Colors.SteelBlue, Size = 20 };
            AddSeatInternal(seat);
        }

        private void El_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse el && el.Tag is Seat seat)
            {
                if (RowToolToggle.IsChecked == true)
                {
                    // If the Row tool is active, clicking on a seat toggles its selection but doesn't start moving
                    SelectionService?.Toggle(seat);
                    e.Handled = true;
                    return;
                }

                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    SelectionService?.Toggle(seat);
                }
                else
                {
                    // If clicking an unselected seat, select it
                    if (!seat.IsSelected)
                        SelectionService?.Select(seat);
                }

                // Start dragging if the clicked seat is part of the selection
                if (seat.IsSelected)
                {
                    _isDraggingSeats = true;
                    _dragStartWorld = ZoomCanvas.ScreenToWorldPoint(e.GetPosition(ZoomCanvas));
                    _dragOriginalPositions = new Dictionary<Seat, Point>();
                    foreach (var obj in SelectionService?.SelectedItems ?? System.Linq.Enumerable.Empty<object>())
                    {
                        if (obj is Seat s)
                            _dragOriginalPositions[s] = s.Position;
                    }

                    ZoomCanvas.Canvas.CaptureMouse();
                }

                e.Handled = true;
            }
        }

        private void UpdateSelectionVisual(Seat seat)
        {
            if (!_seatElements.TryGetValue(seat, out var el)) return;

            if (seat.IsSelected)
            {
                el.Stroke = Brushes.Gold;
                el.StrokeThickness = 3;
            }
            else
            {
                el.Stroke = Brushes.Transparent;
                el.StrokeThickness = 0;
            }
        }

        /// <summary>
        /// Cancels any active tool or in-progress operation (row placement, add-seat, dragging, selection box)
        /// </summary>
        public void CancelActiveTool()
        {
            // Cancel row placement
            if (_isRowPlacing)
            {
                _isRowPlacing = false;
                if (_rowPreviewLine != null) ZoomCanvas.Canvas.Children.Remove(_rowPreviewLine);
                _rowPreviewLine = null;
                foreach (var p in _rowPreviewSeats) ZoomCanvas.Canvas.Children.Remove(p);
                _rowPreviewSeats.Clear();
                ZoomCanvas.Canvas.ReleaseMouseCapture();
            }

            // Cancel add seat mode
            AddSeatToggle.IsChecked = false;

            // Cancel selection rectangle
            if (_isSelecting)
            {
                _isSelecting = false;
                if (_rubberRect != null)
                {
                    ZoomCanvas.Canvas.Children.Remove(_rubberRect);
                    _rubberRect = null;
                }
                ZoomCanvas.Canvas.ReleaseMouseCapture();
            }

            // Cancel dragging and restore original positions
            if (_isDraggingSeats && _dragOriginalPositions != null)
            {
                foreach (var kv in _dragOriginalPositions)
                {
                    var seat = kv.Key;
                    var orig = kv.Value;
                    seat.Position = orig;
                    if (_seatElements.TryGetValue(seat, out var el))
                    {
                        Canvas.SetLeft(el, orig.X - el.Width / 2);
                        Canvas.SetTop(el, orig.Y - el.Height / 2);
                    }
                }

                _isDraggingSeats = false;
                _dragOriginalPositions = null;
                ZoomCanvas.Canvas.ReleaseMouseCapture();
            }
        }

        public void DeleteSelection()
        {
            var seatsToDelete = new List<Seat>();
            foreach (var obj in SelectionService?.SelectedItems ?? System.Linq.Enumerable.Empty<object>()) if (obj is Seat s) seatsToDelete.Add(s);

            if (seatsToDelete.Count == 0) return;

            if (UndoService != null)
            {
                UndoService.Do(new DeleteSeatsAction(this, seatsToDelete));
            }
            else
            {
                foreach (var s in seatsToDelete) RemoveSeatInternal(s);
            }

            SelectionService?.Clear();
        }
    }
}