using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomControls.Controls
{
    public class PoseData
    {
        public string Station { get; set; } = "";
        public string Finger { get; set; } = "";

        // J1..J6 关节 / J7,J8 用作底座角度或备用
        public double J1 { get; set; }
        public double J2 { get; set; }
        public double J3 { get; set; }
        public double J4 { get; set; }
        public double J5 { get; set; }
        public double J6 { get; set; }
        public double J7 { get; set; } // base for fingerB or misc
        public double J8 { get; set; } // base for fingerA or misc
    }
}
