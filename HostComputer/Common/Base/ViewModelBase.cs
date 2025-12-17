using HostComputer.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Base
{
    /// <summary>
    /// 所有 ViewModel 的统一基类
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => Set(ref _isBusy, value);
        }

        public virtual void OnLoaded() { }
        public virtual void OnUnloaded() { }
    }
}
