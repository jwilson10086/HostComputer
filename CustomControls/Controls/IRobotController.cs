using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomControls.Controls
{
    public interface IRobotController
    {
        Task Pick(string finger, string station);
        Task Place(string finger, string station);
        Task Home();
    }

}
