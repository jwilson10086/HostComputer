using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Base
{
    /// <summary>
    /// 所有 Action 的基类
    /// </summary>
    public abstract class ActionBase
    {
        public abstract void Invoke(object? parameter);
    }
}
