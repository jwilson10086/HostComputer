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
            set { _userName = value; this.NotifyChanged(); }
        }

        private string _password = "";

        public string Password
        {
            get { return _password; }
            set { _password = value; this.NotifyChanged(); }
        }

        public int Level { get; set; }
        public string Group { get; set; }

    }
}
