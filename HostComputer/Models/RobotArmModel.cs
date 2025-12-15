using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class RobotArmModel
    {
        // 简化为 2D 关节链（肩-肘-爪）
        public double BaseAngle { get; set; } = 0; // degrees
        public double ElbowAngle { get; set; } = 0;
        public double WristAngle { get; set; } = 0;

        // gripper open fraction 0..1
        public double Gripper { get; set; } = 0;

        // Cartesian position for convenience (computed)
        public (double X, double Y) EndEffectorPos { get; private set; } = (0, 0);

        private double L1 = 100, L2 = 80, L3 = 30; // link lengths in px for UI

        public void UpdateKinematics()
        {
            // compute forward kinematics
            double a1 = DegToRad(BaseAngle);
            double a2 = DegToRad(ElbowAngle);
            double a3 = DegToRad(WristAngle);

            double x = L1 * Math.Cos(a1) + L2 * Math.Cos(a1 + a2) + L3 * Math.Cos(a1 + a2 + a3);
            double y = L1 * Math.Sin(a1) + L2 * Math.Sin(a1 + a2) + L3 * Math.Sin(a1 + a2 + a3);
            EndEffectorPos = (x, y);
        }

        private double DegToRad(double d) => d * Math.PI / 180.0;
    }
}
