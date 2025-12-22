using HostComputer.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Behaviors
{
    /// <summary>
    /// 直接调用方法（不走 ICommand）
    /// </summary>
    public class CallMethodAction : ActionBase
    {
        public string MethodName { get; set; } = string.Empty;

        public object? TargetObject { get; set; }

        public override void Invoke(object? parameter)
        {
            var target = TargetObject ?? parameter;
            if (target == null)
                return;

            var method = target.GetType().GetMethod(
                MethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            method?.Invoke(target, null);
        }
    }
}

