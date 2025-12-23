using HostComputer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Services.Components
{
    public class LayoutConfig
    {
        public string SourceViewName { get; set; } = "MianWindow";
        public List<DeviceItemModel> Devices { get; set; } = new();
    }

}
