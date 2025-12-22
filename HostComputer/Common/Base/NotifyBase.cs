using HostComputer.Common.Base;
using System;                           // 引用 System 命名空间，提供基础类和常用数据类型
using System.Collections.Generic;       // 引用集合相关的命名空间（本文件中没用到，但常用于集合类型）
using System.ComponentModel;            // 引用组件模型命名空间，包含 INotifyPropertyChanged 接口
using System.Linq;                      // 引用 LINQ 相关的命名空间（本文件中没用到）
using System.Runtime.CompilerServices;  // 引用编译器服务命名空间，提供 CallerMemberName 特性
using System.Text;                      // 引用文本处理相关命名空间（本文件中没用到）
using System.Threading.Tasks;           // 引用异步和多线程相关命名空间（本文件中没用到）

namespace HostComputer.Common.Base
{
    /// <summary>
    /// ⚠ 历史兼容基类（不推荐新代码使用）
    /// </summary>
    public abstract class NotifyBase : ObservableObject
    {
        protected void NotifyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            Raise(propertyName);
        }

        protected bool Set<T>(
            ref T storage,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            return Set(ref storage, value, propertyName);
        }
    }
}
