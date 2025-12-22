using HostComputer.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HostComputer.Common.Behaviors
{
    /// <summary>
    /// 调用 ICommand 的 Action
    /// </summary>
    public class InvokeCommandAction : ActionBase
    {
        public ICommand? Command { get; set; }

        public object? CommandParameter { get; set; }

        public override void Invoke(object? parameter)
        {
            var param = CommandParameter ?? parameter;
            if (Command?.CanExecute(param) == true)
            {
                Command.Execute(param);
            }
        }
    }
}

