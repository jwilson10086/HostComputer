using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class SensorModule
    {
        public PumpModule Pump { get; set; }
        public double ReadPressure() => Pump?.Pressure ?? 0;
    }
}
