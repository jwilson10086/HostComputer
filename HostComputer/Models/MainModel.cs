using HostComputer.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HostComputer.Models
{
    public class MainModel : NotifyBase
    {
        private string _time;

        public string Time
        {
            get { return _time; }
            set { _time = value; this.NotifyChanged(); }
        }

        private string _userName;

        public string UserName
        {
            get { return _userName; }
            set { _userName = value; this.NotifyChanged(); }
        }

        private string _avatar;

        public string Avatar
        {
            get { return _avatar; }
            set { _avatar = value; this.NotifyChanged(); }
        }

        private UIElement _mainContent;

        public UIElement MainContent
        {
            get { return _mainContent; }
            set { _mainContent = value; this.NotifyChanged(); }
        }

    }
}
