using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Services.Components
{
    public class PageSnapshot
    {
        public string PageName { get; set; } = "";
        public List<string> Components { get; set; } = new();
    }

}
