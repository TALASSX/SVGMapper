using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SVGMapper.Converters
{
    public class HexColorConverter : IValueConverter
    {
        // Convert Color -> hex string (#RRGGBB)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c)
                return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            return "#000000";
        }

        // Convert hex string -> Color
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                try
                {
                    if (s.StartsWith("#")) s = s.Substring(1);
                    byte r = 0, g = 0, b = 0;
                    if (s.Length == 6)
                    {
                        r = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
                        g = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
                        b = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
                        return Color.FromRgb(r, g, b);
                    }
                }
                catch { }
            }
            return Colors.Black;
        }
    }
}