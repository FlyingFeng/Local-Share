using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LocalShare.Desktop.LocalConverters
{
    public class FileTaskStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int state)
            {
                if (state == 0)
                {
                    return "等待发送";
                }
                else if (state == 1)
                {
                    return "发送中";
                }
                else if (state == 2)
                {
                    return "已停止";
                }
                else if (state == 3)
                {
                    return "已完成";
                }
                else if (state == 4)
                {
                    return "发送出错";
                }
            }

            return "---";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
