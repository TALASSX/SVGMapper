using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SVGMapper.Controls
{
    public partial class ZoomableCanvas : UserControl
    {
        private readonly ScaleTransform _scale = new ScaleTransform(1, 1);
        private readonly TranslateTransform _translate = new TranslateTransform(0, 0);
        private readonly TransformGroup _transformGroup = new TransformGroup();

        // Event fired when the zoom scale changes
        public event Action<double>? ScaleChanged;
        public Canvas Canvas => ContentCanvas;
        public Canvas GridLayer => GridCanvas;

        public static readonly DependencyProperty GridSizeProperty = DependencyProperty.Register(
            nameof(GridSize), typeof(double), typeof(ZoomableCanvas), new PropertyMetadata(25.0, OnGridPropertyChanged));

        public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
            nameof(ShowGrid), typeof(bool), typeof(ZoomableCanvas), new PropertyMetadata(true, OnGridPropertyChanged));

        public static readonly DependencyProperty GridLineBrushProperty = DependencyProperty.Register(
            nameof(GridLineBrush), typeof(Brush), typeof(ZoomableCanvas), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), OnGridPropertyChanged));

        public static readonly DependencyProperty GridOpacityProperty = DependencyProperty.Register(
            nameof(GridOpacity), typeof(double), typeof(ZoomableCanvas), new PropertyMetadata(0.25, OnGridPropertyChanged));

        public double GridSize
        {
            get => (double)GetValue(GridSizeProperty);
            set => SetValue(GridSizeProperty, value);
        }

        public bool ShowGrid
        {
            get => (bool)GetValue(ShowGridProperty);
            set => SetValue(ShowGridProperty, value);
        }

        public Brush GridLineBrush
        {
            get => (Brush)GetValue(GridLineBrushProperty);
            set => SetValue(GridLineBrushProperty, value);
        }

        public double GridOpacity
        {
            get => (double)GetValue(GridOpacityProperty);
            set => SetValue(GridOpacityProperty, value);
        }

        public static readonly DependencyProperty SnapToGridProperty = DependencyProperty.Register(
            nameof(SnapToGrid), typeof(bool), typeof(ZoomableCanvas), new PropertyMetadata(false));

        public static readonly DependencyProperty SnapToleranceProperty = DependencyProperty.Register(
            nameof(SnapTolerance), typeof(double), typeof(ZoomableCanvas), new PropertyMetadata(8.0));

        public bool SnapToGrid
        {
            get => (bool)GetValue(SnapToGridProperty);
            set => SetValue(SnapToGridProperty, value);
        }

        public double SnapTolerance
        {
            get => (double)GetValue(SnapToleranceProperty);
            set => SetValue(SnapToleranceProperty, value);
        }

        private Point? _lastPanPoint;

        public ZoomableCanvas()
        {
            InitializeComponent();

            _transformGroup.Children.Add(_scale);
            _transformGroup.Children.Add(_translate);

            ContentCanvas.RenderTransform = _transformGroup;
            GridCanvas.RenderTransform = _transformGroup;

            // Mouse & wheel handlers
            ContentCanvas.MouseWheel += OnMouseWheel;
            ContentCanvas.MouseDown += OnMouseDown;
            ContentCanvas.MouseUp += OnMouseUp;
            ContentCanvas.MouseMove += OnMouseMove;

            SizeChanged += (s, e) => DrawGrid();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Middle-button drag OR Space + left-drag to pan
            if (e.MiddleButton == MouseButtonState.Pressed || (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.Space)))
            {
                _lastPanPoint = e.GetPosition(this);
                ContentCanvas.CaptureMouse();
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_lastPanPoint.HasValue)
            {
                _lastPanPoint = null;
                ContentCanvas.ReleaseMouseCapture();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_lastPanPoint.HasValue && e.LeftButton == MouseButtonState.Pressed)
            {
                var p = e.GetPosition(this);
                var dx = p.X - _lastPanPoint.Value.X;
                var dy = p.Y - _lastPanPoint.Value.Y;
                _translate.X += dx;
                _translate.Y += dy;
                _lastPanPoint = p;
                DrawGrid();
            }
        }

        private void ZoomIn(object sender, RoutedEventArgs e) => ZoomBy(1.1);
        private void ZoomOut(object sender, RoutedEventArgs e) => ZoomBy(1.0 / 1.1);

    // Public zoom API used by keyboard commands
    public void ZoomInPublic() => ZoomBy(1.1);
    public void ZoomOutPublic() => ZoomBy(1.0 / 1.1);

    private void ZoomBy(double factor)
    {
        var center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
        var worldBefore = ScreenToWorld(center);

        _scale.ScaleX *= factor;
        _scale.ScaleY *= factor;

        var worldAfter = ScreenToWorld(center);

        // Adjust translation so the center point remains stable
        _translate.X += (worldAfter.X - worldBefore.X) * _scale.ScaleX;
        _translate.Y += (worldAfter.Y - worldBefore.Y) * _scale.ScaleY;

        DrawGrid();
        ApplyStrokeScaling();

        ScaleChanged?.Invoke(_scale.ScaleX);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? 1.1 : (1.0 / 1.1);
        var pos = e.GetPosition(this);
        var worldBefore = ScreenToWorld(pos);

        _scale.ScaleX *= factor;
        _scale.ScaleY *= factor;

        var worldAfter = ScreenToWorld(pos);

        _translate.X += (worldAfter.X - worldBefore.X) * _scale.ScaleX;
        _translate.Y += (worldAfter.Y - worldBefore.Y) * _scale.ScaleY;

        DrawGrid();
        ApplyStrokeScaling();
        ScaleChanged?.Invoke(_scale.ScaleX);
    }

        private Point ScreenToWorld(Point screen)
        {
            // world = (screen - translate) / scale
            return new Point((screen.X - _translate.X) / _scale.ScaleX, (screen.Y - _translate.Y) / _scale.ScaleY);
        }

        // Public wrapper so other controls can convert screen -> world coordinates
        public Point ScreenToWorldPoint(Point screen) => ScreenToWorld(screen);

        /// <summary>
        /// If snapping is enabled, returns the nearest grid-aligned point (within SnapTolerance). Otherwise returns the original world point.
        /// </summary>
        public Point SnapToGridPoint(Point worldPoint)
        {
            if (!SnapToGrid || GridSize <= 0) return worldPoint;

            var gx = Math.Round(worldPoint.X / GridSize) * GridSize;
            var gy = Math.Round(worldPoint.Y / GridSize) * GridSize;

            var dx = worldPoint.X - gx;
            var dy = worldPoint.Y - gy;
            var dist = Math.Sqrt(dx * dx + dy * dy);

            return dist <= SnapTolerance ? new Point(gx, gy) : worldPoint;
        }

        private static void OnGridPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZoomableCanvas z) z.DrawGrid();
        }

        private void DrawGrid()
        {
            GridCanvas.Children.Clear();

            if (!ShowGrid || GridSize <= 0 || double.IsNaN(ActualWidth) || double.IsNaN(ActualHeight))
                return;

            // Compute visible bounds in world coordinates
            var topLeft = ScreenToWorld(new Point(0, 0));
            var bottomRight = ScreenToWorld(new Point(ActualWidth, ActualHeight));

            var xmin = Math.Floor(Math.Min(topLeft.X, bottomRight.X) / GridSize) * GridSize;
            var xmax = Math.Ceiling(Math.Max(topLeft.X, bottomRight.X) / GridSize) * GridSize;
            var ymin = Math.Floor(Math.Min(topLeft.Y, bottomRight.Y) / GridSize) * GridSize;
            var ymax = Math.Ceiling(Math.Max(topLeft.Y, bottomRight.Y) / GridSize) * GridSize;

            // Prepare brush with configured opacity
            Brush brush = GridLineBrush;
            if (brush is SolidColorBrush scb)
            {
                var color = scb.Color;
                brush = new SolidColorBrush(Color.FromArgb((byte)(GridOpacity * 255), color.R, color.G, color.B));
            }

            // Draw vertical lines
            for (double x = xmin; x <= xmax; x += GridSize)
            {
                var line = new Line
                {
                    X1 = x,
                    Y1 = ymin,
                    X2 = x,
                    Y2 = ymax,
                    Stroke = brush,
                    StrokeThickness = 0.5
                };
                GridCanvas.Children.Add(line);
            }

            // Draw horizontal lines
            for (double y = ymin; y <= ymax; y += GridSize)
            {
                var line = new Line
                {
                    X1 = xmin,
                    Y1 = y,
                    X2 = xmax,
                    Y2 = y,
                    Stroke = brush,
                    StrokeThickness = 0.5
                };
                GridCanvas.Children.Add(line);
            }
        }

        private static readonly DependencyProperty OriginalStrokeThicknessProperty = DependencyProperty.RegisterAttached(
            "OriginalStrokeThickness",
            typeof(double),
            typeof(ZoomableCanvas),
            new PropertyMetadata(double.NaN));

        private static void SetOriginalStrokeThickness(System.Windows.DependencyObject obj, double value) => obj.SetValue(OriginalStrokeThicknessProperty, value);
        private static double GetOriginalStrokeThickness(System.Windows.DependencyObject obj) => (double)obj.GetValue(OriginalStrokeThicknessProperty);

        /// <summary>
        /// Ensures stroke thickness stays visually consistent by scaling stroke thickness inversely to zoom scale.
        /// </summary>
        private void ApplyStrokeScaling()
        {
            if (double.IsNaN(_scale.ScaleX) || _scale.ScaleX <= 0) return;
            var inverse = 1.0 / _scale.ScaleX;

            foreach (var child in ContentCanvas.Children)
            {
                if (child is Shape shape)
                {
                    var orig = GetOriginalStrokeThickness(shape);
                    if (double.IsNaN(orig))
                    {
                        orig = shape.StrokeThickness;
                        SetOriginalStrokeThickness(shape, orig);
                    }

                    shape.StrokeThickness = Math.Max(0.2, orig * inverse);
                }
            }
        }

        private void GridToggle_Checked(object sender, RoutedEventArgs e) => ShowGrid = true;
        private void GridToggle_Unchecked(object sender, RoutedEventArgs e) => ShowGrid = false;

        private void SnapToggle_Checked(object sender, RoutedEventArgs e) => SnapToGrid = true;
        private void SnapToggle_Unchecked(object sender, RoutedEventArgs e) => SnapToGrid = false;
    }
}