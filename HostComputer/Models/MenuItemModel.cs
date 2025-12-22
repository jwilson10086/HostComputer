using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HostComputer.Common.Base;

namespace HostComputer.Models
{
    public class MenuItemModel : ObservableObject
    {
        public string Key { get; set; }
        public string ViewName { get; set; }
        public string Icon { get; set; }
        public int RequiredLevel { get; set; } = 0; // 最低权限
        public string PermissionKey { get; set; } = "";

        public ObservableCollection<MenuItemModel> Children { get; set; } = new();

        private string _title;
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => Set(ref _isEnabled, value);
        }
    }

}
