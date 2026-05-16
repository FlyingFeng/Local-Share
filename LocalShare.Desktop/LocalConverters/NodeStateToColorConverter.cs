using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LocalShare.Desktop.LocalConverters
{
    public class NodeStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int state)
            {
                if (state == 0)
                {
                    return UIShared.Green;
                }
                else if (state == 1)
                {
                    return UIShared.Red;
                }
            }
            return UIShared.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
