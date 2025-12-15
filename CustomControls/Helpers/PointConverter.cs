using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CustomControls.Helpers
{
    // IValueConverter 接口用于将数据从一种类型转换为另一种类型
    public class PointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Point point)
            {
                if (parameter != null && double.TryParse(parameter.ToString(), out double offset))
                {
                    // 如果有偏移量参数，则将点坐标加上偏移量
                    return point.X + offset;
                }

                // 默认返回 X 坐标
                return point.X;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 这个方法在这里用不到，所以可以不做实现
            throw new NotImplementedException();
        }
    }
}
