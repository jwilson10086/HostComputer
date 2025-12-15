using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class AxisModule
    {
        public enum AxisState { Idle, MovingUp, MovingDown }
        public AxisState State { get; private set; } = AxisState.Idle;
        public double Position { get; private set; } = 0.0; // 0..100 %
        public double SpeedPercentPerSec { get; set; } = 10.0;

        public void MoveUp() => State = AxisState.MovingUp;
        public void MoveDown() => State = AxisState.MovingDown;
        public void Stop() => State = AxisState.Idle;

        public void Tick(double dt)
        {
            double delta = SpeedPercentPerSec * dt;
            if (State == AxisState.MovingUp)
            {
                Position += delta;
                if (Position >= 100) { Position = 100; State = AxisState.Idle; }
            }
            else if (State == AxisState.MovingDown)
            {
                Position -= delta;
                if (Position <= 0) { Position = 0; State = AxisState.Idle; }
            }
        }
    }
}
