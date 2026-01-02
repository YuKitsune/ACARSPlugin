using System.Globalization;
using System.Windows.Data;

namespace CPDLCPlugin.Converters;

[ValueConversion(typeof(int), typeof(int))]
public class AddOneConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i + 1,
            _ => 1
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i - 1,
            _ => 0
        };
    }
}
