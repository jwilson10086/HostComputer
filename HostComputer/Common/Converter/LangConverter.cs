using System;
using System.Globalization;
using System.Windows.Data;
using HostComputer.Common.Services;

namespace HostComputer.Common.Converter
{
    public class LangConverter : IValueConverter
    {
        // 使用 LanguageService 来根据 key 获取对应的翻译
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key)
            {
                var langService = App.Lang; // 获取语言服务
                return langService[key]; // 获取翻译
            }
            return value; // 默认返回原始值
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 不支持反向转换
            throw new NotImplementedException();
        }
    }
}
