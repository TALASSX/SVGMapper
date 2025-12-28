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

                // Map original image-pixel points through the UI transform, apply the UI translation/scale
                // (based on overlay control movement/resizing), then inverse-map back to image pixels.
                var wnd = Window.GetWindow(this) as MainWindow;
                var vm = wnd?.DataContext as MainViewModel;
                System.Windows.Size imgPxSize = new(1, 1);
                System.Windows.Size controlSize = new(1, 1);
                double dpiX = 1.0, dpiY = 1.0;

                // Prefer actual BgImage control size and bitmap DPI when available
                var bgImage = wnd?.FindName("BgImage") as System.Windows.Controls.Image;
                var mainCanvas = wnd?.FindName("MainCanvas") as System.Windows.Controls.Canvas;
                var bmp = bgImage?.Source as System.Windows.Media.Imaging.BitmapSource;
                if (bmp != null && bgImage != null)
                {
                    imgPxSize = new System.Windows.Size(bmp.PixelWidth, bmp.PixelHeight);
                    // If the background image fills the canvas, prefer using the MainCanvas size
                    // to avoid letterbox/offset math. Otherwise use the BgImage display size.
                    if (mainCanvas != null && Math.Abs(bgImage.ActualWidth - mainCanvas.ActualWidth) < 1.0 && Math.Abs(bgImage.ActualHeight - mainCanvas.ActualHeight) < 1.0)
                    {
                        controlSize = new System.Windows.Size(mainCanvas.ActualWidth, mainCanvas.ActualHeight);
                    }
                    else if (bgImage.ActualWidth > 0 && bgImage.ActualHeight > 0)
                    {
                        controlSize = new System.Windows.Size(bgImage.ActualWidth, bgImage.ActualHeight);
                    }
                    else if (mainCanvas != null)
                    {
                        controlSize = new System.Windows.Size(mainCanvas.ActualWidth, mainCanvas.ActualHeight);
                    }
                    else
                    {
                        controlSize = new System.Windows.Size(bgImage.ActualWidth, bgImage.ActualHeight);
                    }
                    dpiX = bmp.DpiX > 0 ? bmp.DpiX / 96.0 : 1.0;
                    dpiY = bmp.DpiY > 0 ? bmp.DpiY / 96.0 : 1.0;
                }
                else if (vm?.Document != null && vm.Document.BackgroundImageWidth > 0 && vm.Document.BackgroundImageHeight > 0)
                {
                    imgPxSize = new System.Windows.Size(vm.Document.BackgroundImageWidth, vm.Document.BackgroundImageHeight);
                    // fallback: if no UI size known, use image pixel size as DIP control size (best-effort)
                    controlSize = imgPxSize;
                    dpiX = vm.Document.BackgroundDpiScaleX > 0 ? vm.Document.BackgroundDpiScaleX : 1.0;
                    dpiY = vm.Document.BackgroundDpiScaleY > 0 ? vm.Document.BackgroundDpiScaleY : 1.0;
                }

                var t = SVGMapper.Minimal.Services.ImageCoordinateTransformer.CalculateTransform(imgPxSize, controlSize, dpiX, dpiY, SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);

                // For each original image pixel point: map -> UI (relative to BgImage),
                // convert to MainCanvas coords so it's in same space as overlay bounds,
                // apply bounding-box transform (scale+translate), convert back to BgImage coords,
                // then invert UI->image pixels.
                // mainCanvas already obtained above
                var bgImageCtrl = bgImage;
                foreach (var imgPt in before)
                {
                    // 1) image pixels -> UI (DIPs relative to BgImage)
                    var uiPt = SVGMapper.Minimal.Services.ImageCoordinateTransformer.TransformPoint(
                        new System.Windows.Point(imgPt.X, imgPt.Y), imgPxSize, controlSize, dpiX, dpiY,
                        SVGMapper.Minimal.Services.ImageCoordinateTransformer.StretchMode.Uniform);

                    // 2) UI point relative to MainCanvas (absolute on canvas)
                    System.Windows.Point uiAbs = uiPt;
                    if (bgImageCtrl != null && mainCanvas != null)
                    {
                        uiAbs = bgImageCtrl.TranslatePoint(uiPt, mainCanvas);
                    }

                    // 3) compute local position relative to oldRect (both in MainCanvas coords)
                    var localX = uiAbs.X - oldRect.X;
                    var localY = uiAbs.Y - oldRect.Y;
                    var newLocalX = localX * sx;
                    var newLocalY = localY * sy;
                    var newUiAbsX = newRect.X + newLocalX;
                    var newUiAbsY = newRect.Y + newLocalY;

                    // 4) convert new absolute UI point back to BgImage-relative point
                    System.Windows.Point newUiRelativeToBg = new System.Windows.Point(newUiAbsX, newUiAbsY);
                    if (bgImageCtrl != null && mainCanvas != null)
                    {
                        newUiRelativeToBg = mainCanvas.TranslatePoint(new System.Windows.Point(newUiAbsX, newUiAbsY), bgImageCtrl);
                    }

                    // 5) invert UI -> image pixels using the transform 't'
                    double xInImageDip = (newUiRelativeToBg.X - t.OffsetX) / t.ScaleX;
                    double yInImageDip = (newUiRelativeToBg.Y - t.OffsetY) / t.ScaleY;
                    double newPx = xInImageDip * dpiX;
                    double newPy = yInImageDip * dpiY;

                    // Clamp to image bounds
                    newPx = Math.Max(0, Math.Min(imgPxSize.Width - 1, newPx));
                    newPy = Math.Max(0, Math.Min(imgPxSize.Height - 1, newPy));

                    after.Add(new System.Windows.Point(newPx, newPy));
                }

                // apply via UndoRedo service on the main VM so changes are undoable
                if (vm != null)
                {
                    // Prepare normalized lists for undo/redo
                    var beforeNorm = new List<Models.PointModel>();
                    var afterNorm = new List<Models.PointModel>();
                    double imgW = imgPxSize.Width, imgH = imgPxSize.Height;
                    foreach (var p in before) beforeNorm.Add(new Models.PointModel { X = imgW > 0 ? p.X / imgW : 0, Y = imgH > 0 ? p.Y / imgH : 0 });
                    foreach (var p in after) afterNorm.Add(new Models.PointModel { X = imgW > 0 ? p.X / imgW : 0, Y = imgH > 0 ? p.Y / imgH : 0 });

                    vm.UndoRedo.Execute(
                        doAction: () =>
                        {
                            // set new points (pixel coords)
                            Room.Points.Clear();
                            foreach (var p in after) Room.Points.Add(new Models.PointModel { X = p.X, Y = p.Y });
                            // set normalized points
                            Room.NormalizedPoints.Clear();
                            foreach (var np in afterNorm) Room.NormalizedPoints.Add(np);
                        },
                        undoAction: () =>
                        {
                            Room.Points.Clear();
                            foreach (var p in before) Room.Points.Add(new Models.PointModel { X = p.X, Y = p.Y });
                            Room.NormalizedPoints.Clear();
                            foreach (var np in beforeNorm) Room.NormalizedPoints.Add(np);
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
