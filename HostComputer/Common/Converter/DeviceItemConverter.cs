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
          
            var assembly = Assembly.Load("CustomControls");//load the assembly
            Type type = assembly.GetType("CustomControls.Controls." + value.ToString());//get the type
            return Activator.CreateInstance(type)!;//create an instance of the type
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
