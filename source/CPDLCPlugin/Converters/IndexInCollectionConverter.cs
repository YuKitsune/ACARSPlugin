using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace CPDLCPlugin.Converters;

public class IndexInCollectionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] == null || values[1] is not IEnumerable collection)
            return "1";

        var item = values[0];
        var index = 0;

        foreach (var collectionItem in collection)
        {
            if (ReferenceEquals(collectionItem, item))
                return (index + 1).ToString();
            index++;
        }

        return "1";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
