using MauiSqlite.Core.Models;

namespace MauiSqlite.App.Converters;

/// <summary>
/// Maps a <see cref="TodoPriority"/> to an accent color for the list.
/// </summary>
public class PriorityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            TodoPriority.High => Color.FromArgb("#D32F2F"),
            TodoPriority.Low => Color.FromArgb("#388E3C"),
            _ => Color.FromArgb("#1976D2"),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
