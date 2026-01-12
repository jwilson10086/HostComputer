using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomControls.Controls
{
    public class RobotRouteStep
    {
        public string Finger { get; set; }   // FingerA / FingerB
        public PoseData From { get; set; }
        public PoseData To { get; set; }

        public string DeviceNum { get; set; } // 关联的设备编号
    }
}
