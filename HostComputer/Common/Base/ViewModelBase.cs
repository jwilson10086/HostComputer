using HostComputer.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        protected CommandBase CreateCommand(string propertyName, Action<object?> execute)
        {
            var cmd = new CommandBase { DoExecute = execute,Name = propertyName };

            // 绑定 Permission
            var prop = GetType().GetProperty(propertyName);
            var permission = prop?.GetCustomAttribute<PermissionAttribute>();
            cmd.Permission = permission;

            // 注册到全局 CommandRegistry
            CommandRegistry.Register(cmd);

            return cmd;
        }

    }

}
