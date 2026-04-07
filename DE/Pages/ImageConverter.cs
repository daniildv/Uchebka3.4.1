using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DE.Pages
{
    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parametr, CultureInfo culture)
        {
            string ImageName = value as string;

            if (string.IsNullOrEmpty(ImageName))
                return GetPlaceholder();

            try
            {
                string fullPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    ImageName);

                if (File.Exists(fullPath))
                {
                    return new BitmapImage(new Uri(fullPath));
                }

                return GetPlaceholder();
            }
            catch
            {
                return GetPlaceholder();
            }
        }

        private BitmapImage GetPlaceholder()
        {
            try
            {
                string fullPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    "picture.png");

                if (File.Exists(fullPath))
                    return new BitmapImage(new Uri(fullPath));
            }
            catch { }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}