using System.Globalization;
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
                    return "等待传输";
                }
                else if (state == 1)
                {
                    return "传输中";
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
                    return "传输出错";
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
