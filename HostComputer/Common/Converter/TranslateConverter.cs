using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup.Primitives;
using System.Windows.Media;

namespace HostComputer.Common.Converter
{
    public class TranslateConverter : IValueConverter
    {
        public TranslateConverter()
        {
            App.Lang.LanguageChanged += () =>
            {
                // 通知 WPF 所有使用该 Converter 的绑定重新计算
                RefreshAllBindings();
            };
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            return App.Lang[value.ToString()];
        }

        private static void RefreshAllBindings()
        {
            foreach (Window window in Application.Current.Windows)
            {
                RefreshBindings(window);
            }
        }

        private static void RefreshBindings(DependencyObject obj)
        {
            if (obj == null)
                return;

            foreach (var dp in MarkupWriter.GetMarkupObjectFor(obj).Properties)
            {
                if (dp.DependencyProperty != null)
                {
                    var binding = BindingOperations.GetBindingExpression(obj, dp.DependencyProperty);
                    binding?.UpdateTarget();
                }
            }

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                RefreshBindings(VisualTreeHelper.GetChild(obj, i));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
