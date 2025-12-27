namespace SVGMapper.Minimal.Services
{
    public class SnapService
    {
        public (double X, double Y) Snap(double x, double y, double gridSize)
        {
            var gx = Math.Round(x / gridSize) * gridSize;
            var gy = Math.Round(y / gridSize) * gridSize;
            return (gx, gy);
        }
    }
}