using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Base
{
    public class PIDController
    {
        private double Kp, Ki, Kd;
        private double integral = 0;
        private double lastError = 0;
        private double lastOutput = 0;

        public PIDController(double kp, double ki, double kd)
        {
            Kp = kp; Ki = ki; Kd = kd;
        }

        // error = setpoint - measurement or other convention (we used simple)
        public double Update(double error, double dt)
        {
            integral += error * dt;
            var derivative = (error - lastError) / dt;
            var output = Kp * error + Ki * integral + Kd * derivative;
            lastError = error;
            lastOutput = output;
            return output;
        }

        public void Reset() { integral = 0; lastError = 0; lastOutput = 0; }
    }
}
