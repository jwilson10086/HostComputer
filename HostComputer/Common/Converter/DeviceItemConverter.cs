using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HostComputer.Common.Converter
{
    public class DeviceItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var assembly = Assembly.Load("CustomControls");
            string typeName = "CustomControls.Controls." + value.ToString();
            Type? type = assembly.GetType(typeName);

            if (type == null)
                throw new InvalidOperationException($"Type '{typeName}' not found in assembly '{assembly.FullName}'.");

            return Activator.CreateInstance(type)!;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
