using HostComputer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HostComputer.Common.Converter
{
    /// <summary>
    /// EqualityConverter 用于多值绑定时判断两个对象是否相等。
    /// 返回值为布尔值（true/false）。
    /// 常用于 WPF 的 MultiBinding 场景，比如按钮是否启用、控件是否可见等。
    /// </summary>
    public class EqualityConverter : IMultiValueConverter
    {
        /// <summary>
        /// 将绑定的多个值进行转换。
        /// </summary>
        /// <param name="values">绑定的值数组</param>
        /// <param name="targetType">目标属性类型</param>
        /// <param name="parameter">绑定时传入的参数（可选）</param>
        /// <param name="culture">区域性信息</param>
        /// <returns>
        /// 如果 values 至少有两个元素，且 values[0] 与 values[1] 引用相等，则返回 true；
        /// 否则返回 false。
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return false;

            if (values[0] is MenuItemModel m1 && values[1] is MenuItemModel m2)
                return m1.Title == m2.Title;

            return false;
        }


        /// <summary>
        /// 将目标值转换回源值。
        /// 由于本转换器只用于判断是否相等，没有反向逻辑，因此这里直接返回 Binding.DoNothing，
        /// 告诉 WPF 不对源属性进行修改。
        /// </summary>
        /// <param name="value">目标属性的值</param>
        /// <param name="targetTypes">源属性类型数组</param>
        /// <param name="parameter">绑定时传入的参数（可选）</param>
        /// <param name="culture">区域性信息</param>
        /// <returns>返回与 targetTypes 等长的数组，每个元素都为 Binding.DoNothing</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return targetTypes.Select(_ => Binding.DoNothing).ToArray();
        }
    }
}
