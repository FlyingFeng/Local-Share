using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LocalShare.Desktop.LocalConverters
{
    public class EmptyStringToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() ?? "0";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            if (string.IsNullOrWhiteSpace(str)) return 0;  // 空字符串返回默认值
            if (int.TryParse(str, out int result)) return result;
            return DependencyProperty.UnsetValue;  // 解析失败不更新源
        }
    }
}
