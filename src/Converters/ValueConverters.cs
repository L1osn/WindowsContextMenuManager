using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ContextMenuManager.Models;

namespace ContextMenuManager.Converters
{
    /// <summary>Inverts a boolean value.</summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }

    /// <summary>Boolean to Visibility.</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    /// <summary>Risk level to foreground color.</summary>
    public class RiskLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RiskLevel risk)
            {
                return risk switch
                {
                    RiskLevel.Low => new SolidColorBrush(Color.FromRgb(16, 124, 16)),      // green
                    RiskLevel.Medium => new SolidColorBrush(Color.FromRgb(255, 140, 0)),   // orange
                    RiskLevel.High => new SolidColorBrush(Color.FromRgb(209, 52, 56)),     // red
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Risk level to background color.</summary>
    public class RiskLevelToBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RiskLevel risk)
            {
                return risk switch
                {
                    RiskLevel.Low => new SolidColorBrush(Color.FromArgb(30, 16, 124, 16)),
                    RiskLevel.Medium => new SolidColorBrush(Color.FromArgb(30, 255, 140, 0)),
                    RiskLevel.High => new SolidColorBrush(Color.FromArgb(30, 209, 52, 56)),
                    _ => new SolidColorBrush(Colors.Transparent)
                };
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Menu source to foreground color.</summary>
    public class MenuSourceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MenuSource source)
            {
                return source switch
                {
                    MenuSource.System => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                    MenuSource.ThirdParty => new SolidColorBrush(Color.FromRgb(135, 100, 184)),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Menu source to background color.</summary>
    public class MenuSourceToBgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MenuSource source)
            {
                return source switch
                {
                    MenuSource.System => new SolidColorBrush(Color.FromArgb(25, 0, 120, 212)),
                    MenuSource.ThirdParty => new SolidColorBrush(Color.FromArgb(25, 135, 100, 184)),
                    _ => new SolidColorBrush(Colors.Transparent)
                };
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
