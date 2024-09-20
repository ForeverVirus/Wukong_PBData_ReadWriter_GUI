using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Wukong_PBData_ReadWriter_GUI.Converters;

public class DirtyToForeground : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool isDirty) return null;
        return isDirty ? Brushes.Green : Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush brush) return null;
        return brush.Color != Colors.Black;
    }
}