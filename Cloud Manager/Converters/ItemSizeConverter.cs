using System;
using System.Globalization;
using System.Windows.Data;

namespace Cloud_Manager.Converters
{
    public class ItemSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "-";
            }    
            int tmp = int.Parse(value.ToString());

            if (tmp > 1024)
            {
                tmp /= 1024;
                if (tmp > 1024)
                {
                    tmp /= 1024;
                    if (tmp > 1024)
                    {
                        tmp /= 1024;
                        return tmp + " GB";
                    }
                    else
                    {
                        return tmp + " MB";
                    }
                }
                else
                {
                    return tmp + " KB";
                }
            }
            else
            {
                return tmp + " bytes";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}