using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SVGMapper.Minimal.Models;
using SVGMapper.Minimal.ViewModels;

namespace SVGMapper.Minimal.Controls
{
    public partial class RoomOverlayControl : UserControl
    {
        private bool isDragging = false;
        private bool isResizing = false;
        private Point dragStart;
        private FrameworkElement? activeHandle;
        private double originalWidth, originalHeight;
        private Point originalPos;
        private System.Windows.Rect? originalBounds;
        private List<System.Windows.Point>? originalRoomPoints;

        public PolygonRoom? Room { get; set; }

        public RoomOverlayControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            MainBorder.MouseLeftButtonDown += DragStart;
            MainBorder.MouseMove += DragMove;
            MainBorder.MouseLeftButtonUp += DragEnd;

            AttachResizeHandle(TopLeft);
            AttachResizeHandle(TopRight);
            AttachResizeHandle(BottomLeft);
            AttachResizeHandle(BottomRight);
            AttachResizeHandle(Left);
            AttachResizeHandle(Right);
            AttachResizeHandle(Top);
            AttachResizeHandle(Bottom);

            // set label if Room provided
            if (Room != null)
            {
                LabelText.Text = Room.Name;
            }
        }

        private void AttachResizeHandle(FrameworkElement handle)
        {
            handle.MouseLeftButtonDown += ResizeStart;
            handle.MouseMove += ResizeMove;
            handle.MouseLeftButtonUp += ResizeEnd;
        }

        // --- Dragging ---
        private void DragStart(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            var canvas = Parent as Canvas;
            if (canvas == null) return;
            dragStart = e.GetPosition(canvas);
            originalPos = new Point(Canvas.GetLeft(this).DoubleOrZero(), Canvas.GetTop(this).DoubleOrZero());
            // capture original room points and bounds for undoable transform
            if (Room != null)
            {
                originalRoomPoints = Room.Points.Select(p => new System.Windows.Point(p.X, p.Y)).ToList();
                originalBounds = new System.Windows.Rect(originalPos.X, originalPos.Y, Width, Height);
            }
            CaptureMouse();
            e.Handled = true;
        }

        private void DragMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;
            var canvas = Parent as Canvas;
            if (canvas == null) return;
            var pos = e.GetPosition(canvas);

            double dx = pos.X - dragStart.X;
            double dy = pos.Y - dragStart.Y;

            double nx = originalPos.X + dx;
            double ny = originalPos.Y + dy;

            var snap = GetSnapFunction();
            Canvas.SetLeft(this, snap(nx));
            Canvas.SetTop(this, snap(ny));
        }

        private void DragEnd(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ReleaseMouseCapture();
            // apply translation to underlying polygon points
            ApplyTransformToRoomIfNeeded();
            e.Handled = true;
        }

        // --- Resize ---
        private void ResizeStart(object sender, MouseButtonEventArgs e)
        {
            isResizing = true;
            activeHandle = sender as FrameworkElement;
            var canvas = Parent as Canvas;
            if (canvas == null) return;
            dragStart = e.GetPosition(canvas);
            originalWidth = Width;
            originalHeight = Height;
            originalPos = new Point(Canvas.GetLeft(this).DoubleOrZero(), Canvas.GetTop(this).DoubleOrZero());
            // capture original room points and bounds for undoable transform
            if (Room != null)
            {
                originalRoomPoints = Room.Points.Select(p => new System.Windows.Point(p.X, p.Y)).ToList();
                originalBounds = new System.Windows.Rect(originalPos.X, originalPos.Y, originalWidth, originalHeight);
            }
            CaptureMouse();
            e.Handled = true;
        }

        private void ResizeMove(object sender, MouseEventArgs e)
        {
            if (!isResizing || activeHandle == null) return;
            var canvas = Parent as Canvas;
            if (canvas == null) return;
            var pos = e.GetPosition(canvas);

            double dx = pos.X - dragStart.X;
            double dy = pos.Y - dragStart.Y;

            var snap = GetSnapFunction();

            // Corner handles
            if (activeHandle == TopLeft)
            {
                double newW = Math.Max(16, originalWidth - dx);
                double newH = Math.Max(16, originalHeight - dy);
                double newX = originalPos.X + dx;
                double newY = originalPos.Y + dy;
                Width = snap(newW);
                Height = snap(newH);
                Canvas.SetLeft(this, snap(newX));
                Canvas.SetTop(this, snap(newY));
            }
            else if (activeHandle == TopRight)
            {
                double newW = Math.Max(16, originalWidth + dx);
                double newH = Math.Max(16, originalHeight - dy);
                double newY = originalPos.Y + dy;
                Width = snap(newW);
                Height = snap(newH);
                Canvas.SetTop(this, snap(newY));
            }
            else if (activeHandle == BottomLeft)
            {
                double newW = Math.Max(16, originalWidth - dx);
                double newH = Math.Max(16, originalHeight + dy);
                double newX = originalPos.X + dx;
                Width = snap(newW);
                Height = snap(newH);
                Canvas.SetLeft(this, snap(newX));
            }
            else if (activeHandle == BottomRight)
            {
                double newW = Math.Max(16, originalWidth + dx);
                double newH = Math.Max(16, originalHeight + dy);
                Width = snap(newW);
                Height = snap(newH);
            }
            // Side handles
            else if (activeHandle == Left)
            {
                double newW = Math.Max(16, originalWidth - dx);
                double newX = originalPos.X + dx;
                Width = snap(newW);
                Canvas.SetLeft(this, snap(newX));
            }
            else if (activeHandle == Right)
            {
                double newW = Math.Max(16, originalWidth + dx);
                Width = snap(newW);
            }
            else if (activeHandle == Top)
            {
                double newH = Math.Max(16, originalHeight - dy);
                double newY = originalPos.Y + dy;
                Height = snap(newH);
                Canvas.SetTop(this, snap(newY));
            }
            else if (activeHandle == Bottom)
            {
                double newH = Math.Max(16, originalHeight + dy);
                Height = snap(newH);
            }

            e.Handled = true;
        }

        private void ResizeEnd(object sender, MouseButtonEventArgs e)
        {
            isResizing = false;
            activeHandle = null;
            ReleaseMouseCapture();
            // apply resize transform to the underlying polygon points
            ApplyTransformToRoomIfNeeded();
            e.Handled = true;
        }

        private void ApplyTransformToRoomIfNeeded()
        {
            try
            {
                if (Room == null || originalRoomPoints == null || originalBounds == null) return;

                var newLeft = Canvas.GetLeft(this).DoubleOrZero();
                var newTop = Canvas.GetTop(this).DoubleOrZero();
                var newRect = new System.Windows.Rect(newLeft, newTop, Width, Height);
                var oldRect = originalBounds.Value;

                double sx = oldRect.Width != 0 ? newRect.Width / oldRect.Width : 1.0;
                double sy = oldRect.Height != 0 ? newRect.Height / oldRect.Height : 1.0;

                // prepare copies for undo/redo
                var before = originalRoomPoints.Select(pt => new System.Windows.Point(pt.X, pt.Y)).ToList();
                var after = new List<System.Windows.Point>();

                foreach (var pt in before)
                {
                    double relX = oldRect.Width != 0 ? (pt.X - oldRect.X) / oldRect.Width : 0.0;
                    double relY = oldRect.Height != 0 ? (pt.Y - oldRect.Y) / oldRect.Height : 0.0;
                    double nx = newRect.X + relX * newRect.Width;
                    double ny = newRect.Y + relY * newRect.Height;
                    after.Add(new System.Windows.Point(nx, ny));
                }

                // apply via UndoRedo service on the main VM so changes are undoable
                var wnd = Window.GetWindow(this);
                if (wnd?.DataContext is MainViewModel vm)
                {
                    vm.UndoRedo.Execute(
                        doAction: () =>
                        {
                            // set new points
                            Room.Points.Clear();
                            foreach (var p in after) Room.Points.Add(new Models.PointModel { X = p.X, Y = p.Y });
                        },
                        undoAction: () =>
                        {
                            Room.Points.Clear();
                            foreach (var p in before) Room.Points.Add(new Models.PointModel { X = p.X, Y = p.Y });
                        }
                    );
                }

                // clear cached originals
                originalRoomPoints = null;
                originalBounds = null;
            }
            catch { }
        }

        private Func<double,double> GetSnapFunction()
        {
            try
            {
                var wnd = Window.GetWindow(this);
                if (wnd?.DataContext is MainViewModel vm)
                {
                    double g = Math.Max(4.0, vm.GridSize);
                    return v => Math.Round(v / g) * g;
                }
            }
            catch { }
            return v => v; // no snap
        }
    }

    static class Extensions
    {
        public static double DoubleOrZero(this double? d) => d ?? 0.0;
        public static double DoubleOrZero(this double d) => d;
        public static double DoubleOrZero(this object? o)
        {
            if (o is double dd) return dd;
            return 0.0;
        }
    }
}
