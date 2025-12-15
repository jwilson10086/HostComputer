using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class UltrasonicModule
    {
        public bool IsOn { get; set; } = false;
        public double Power { get; set; } = 0; // 0..100 %
        public double TemperatureGainPerSec => (Power / 100.0) * 0.2; // °C per sec

        public void Tick(double dt)
        {
            // nothing stateful yet; used by Tank to add heat
        }
    }
}
