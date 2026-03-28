using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace EduMindAI.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 录音时亮红色，未录音时亮灰色
        return value is true
            ? new SolidColorBrush(Color.Parse("#FF6666"))   // 亮红色
            : new SolidColorBrush(Color.Parse("#AAAAAA")); // 亮灰色
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}