using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HostComputer.Common.Base
{
    /// <summary>
    /// MVVM 核心：属性变更通知（唯一实现）
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void Raise([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }

        protected virtual void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
