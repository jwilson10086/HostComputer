using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using HostComputer.Common.Base;

namespace HostComputer.Models
{
    public class ThumbModel 
    {
        public string Header { get; set; }
        public List<ThumbItemModel> Children { get; set; }
    }

    public class ThumbItemModel
    {
        public string Icon { get; set; }
        public string TargetType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public ICommand ThumbCommand { get; set; }
    }
}
