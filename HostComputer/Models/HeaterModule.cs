using HostComputer.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class HeaterModule
    {
        public bool HeaterOn { get; set; } = false;
        public double TargetTemp { get; set; } = 25;
        public double Temp { get; set; } = 25; // current tank temperature °C
        public double HeaterPower { get; private set; } = 0; // 0..1
        public PIDController PID { get; private set; }

        public HeaterModule()
        {
            PID = new PIDController(1.0, 0.05, 0.1); // 原始参数，可调
        }

        public void Tick(double dt, double ambientTemp = 25.0, double extraHeat = 0.0)
        {
            // PID computes power towards target
            var p = PID.Update(TargetTemp - Temp, dt);
            HeaterPower = Math.Max(0, Math.Min(1, p));
            if (!HeaterOn) HeaterPower = 0;

            // first-order heating and cooling
            double heatGain = HeaterPower * 300.0 * dt; // arbitrary scale
            Temp += heatGain * 0.001; // convert to °C
            // ultrasonic adds small heat
            Temp += extraHeat * dt;
            // passive cooling
            Temp += (ambientTemp - Temp) * (1 - Math.Exp(-dt / 60.0));
        }
    }
}
