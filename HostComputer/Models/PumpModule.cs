using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class PumpModule
    {
        public bool IsRunning { get; private set; } = false;
        public double SpeedSet { get; set; } = 0;   // 0-100 %
        public double SpeedFeedback { get; private set; } = 0;
        public double Pressure { get; private set; } = 0; // bar
        public double MaxPressure { get; set; } = 2.0; // bar

        public void Start() => IsRunning = true;
        public void Stop() { IsRunning = false; SpeedSet = 0; }

        public void Tick(double dt)
        {
            // first-order response for speed
            SpeedFeedback += (SpeedSet - SpeedFeedback) * Math.Min(1.0, 0.5 * dt * 10.0); // smoothing
            if (!IsRunning) SpeedFeedback = 0;

            // pressure roughly proportional to speed with some nonlinearity
            Pressure = (SpeedFeedback / 100.0) * MaxPressure * (0.9 + 0.2 * Math.Sin(DateTime.Now.Millisecond / 1000.0));
            if (!IsRunning) Pressure = 0;
        }
    }
}
