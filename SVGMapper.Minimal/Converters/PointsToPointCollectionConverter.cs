using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SVGMapper.Minimal.Models;

namespace SVGMapper.Minimal.Converters
{
    public class PointsToPointCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var pc = new PointCollection();
            if (value is ObservableCollection<PointModel> pts)
            {
                foreach (var p in pts)
                    pc.Add(new System.Windows.Point(p.X, p.Y));
            }
            return pc;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}