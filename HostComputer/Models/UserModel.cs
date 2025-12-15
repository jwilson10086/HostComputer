using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HostComputer.Common.Base;

namespace HostComputer.Models
{
    public class UserModel : NotifyBase
    {
        private string _userName;

        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                this.NotifyChanged();
            }
        }

        private string _password = "";

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                this.NotifyChanged();
            }
        }

        private int _level;
        public int Level
        {
            get { return _level; }
            set
            {
                _level = value;
                this.NotifyChanged();
            }
        }
        private string _group;
        public string Group
        {
            get { return _group; }
            set
            {
                _group = value;
                this.NotifyChanged();
            }
        }
    }
}
