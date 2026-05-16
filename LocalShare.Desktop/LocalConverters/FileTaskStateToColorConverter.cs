using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LocalShare.Desktop.LocalConverters
{
    public class FileTaskStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int state)
            {
                if (state == 0)
                {
                    return UIShared.Black;
                    //return "等待传输";
                }
                else if (state == 1)
                {
                    return UIShared.Blue;
                    //return "传输中";
                }
                else if (state == 2)
                {
                    return UIShared.Yellow;
                    //return "已停止";
                }
                else if (state == 3)
                {
                    return UIShared.Green;
                    //return "已完成";
                }
                else if (state == 4)
                {
                    return UIShared.Red;
                    //return "传输出错";
                }
            }

            return UIShared.Black;
            //return "---";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
