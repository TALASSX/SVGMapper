using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SVGMapper.Minimal.Converters
{
    public class GridSizeToRectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && d > 0)
            {
                return new Rect(0, 0, d, d);
            }
            return new Rect(0, 0, 40, 40);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Rect r) return r.Width;
            return Binding.DoNothing;
        }
    }
}
