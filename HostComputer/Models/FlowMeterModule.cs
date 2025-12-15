using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class FlowMeterModule
    {
        public PumpModule Pump { get; set; }
        public bool ValveOpen { get; set; } = true;
        public double Flow { get; private set; } = 0; // L/min
        public double MaxFlow { get; set; } = 20; // L/min

        public void Tick(double dt)
        {
            if (Pump == null || !ValveOpen || !Pump.IsRunning) { Flow = 0; return; }
            // simple proportional
            Flow = Pump.SpeedFeedback / 100.0 * MaxFlow;
            // noise
            Flow += (new Random().NextDouble() - 0.5) * 0.02 * MaxFlow;
            if (Flow < 0) Flow = 0;
        }
    }
}
