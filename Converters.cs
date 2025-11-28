using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WpfApp3
{
    // public — чтобы XAML мог найти тип
    public class DiscountGreaterThan15Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (double.TryParse(value.ToString(), out double d))
            {
                return d > 15.0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class HasDiscountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (double.TryParse(value.ToString(), out double d))
            {
                return d > 0.0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    // MultiValueConverter: принимает Price и Discount и возвращает итоговую цену (double)
    public class DiscountToFinalPriceMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return Binding.DoNothing; ;

            if (double.TryParse(values[0]?.ToString() ?? "0", out double price) &&
                double.TryParse(values[1]?.ToString() ?? "0", out double discount))
            {
                double final = price * (1 - discount / 100.0);
                return final;
            }

            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value?.ToString() ?? "";
            if (!File.Exists(path))
                path = "pack://application:,,,/Assets/Images/placeholder.png";
            return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
