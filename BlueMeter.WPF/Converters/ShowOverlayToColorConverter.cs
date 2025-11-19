using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BlueMeter.WPF.Converters;

/// <summary>
/// Converts ShowOverlay boolean to Color
/// True → White (hide text), False → Black (show text)
/// </summary>
[ValueConversion(typeof(bool), typeof(Color))]
public sealed class ShowOverlayToColorConverter : IMultiValueConverter
{
    public object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 1)
        {
            return Colors.Black;
        }

        var showOverlay = values[0] as bool? ?? false;

        // When overlay is shown, make text white (invisible on white background)
        // When overlay is hidden, make text black (visible)
        return showOverlay ? Colors.White : Colors.Black;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        return [Binding.DoNothing];
    }
}
