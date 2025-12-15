using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class ValveModule
    {
        public bool IsOpen { get; set; } = false;
        public double OpenFraction { get; private set; } = 0; // 0..1

        public void Tick(double dt)
        {
            // smooth open/close
            double target = IsOpen ? 1.0 : 0.0;
            OpenFraction += (target - OpenFraction) * Math.Min(1.0, dt * 5.0);
        }
    }
}
