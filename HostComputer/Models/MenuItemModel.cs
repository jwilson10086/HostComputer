using HostComputer.Common.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Models
{
    public class MenuItemModel : NotifyBase
    {
        public string Key { get; set; }  // 永远是唯一标识，用于语言索引
        public string ViewName { get; set; }
        public ObservableCollection<MenuItemModel> Children { get; set; } = new();

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }

}
