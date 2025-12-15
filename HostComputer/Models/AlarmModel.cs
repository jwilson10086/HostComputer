using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class AlarmModel
    {
        public string AlarmName { get; set; }
        public string AlarmType { get; set; }
        public string AlarmContent { get; set; }
        public DateTime AlarmPostTime { get; set; }
        public DateTime AlarmRecoveryTime { get; set; }
    }
}
