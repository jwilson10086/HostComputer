using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class TankModule
    {
        public double Level { get; private set; } = 100.0; // %
        public HeaterModule Heater { get; set; } = new HeaterModule();
        public UltrasonicModule Ultrasonic { get; set; } = new UltrasonicModule();

        public void Tick(double dt)
        {
            // Heater updates temp; ultrasonic adds extra heat
            double extra = Ultrasonic.IsOn ? Ultrasonic.TemperatureGainPerSec : 0;
            Heater.Tick(dt, 25.0, extra);
        }
    }
}
