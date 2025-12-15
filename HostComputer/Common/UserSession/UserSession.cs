using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HostComputer.Common.Session
{
    public class Session : INotifyPropertyChanged
    {
        private bool _isLogin;
        private string _userName;
        private int _level;
        private string _group;

        public bool IsLogin
        {
            get => _isLogin;
            set { _isLogin = value; OnPropertyChanged(); }
        }

        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }

        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        public string Group
        {
            get => _group;
            set { _group = value; OnPropertyChanged(); }
        }

        public void Clear()
        {
            IsLogin = false;
            UserName = string.Empty;
            Level = 0;
            Group = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
