using System;                           // 引用 System 命名空间，提供基础类和常用数据类型
using System.Collections.Generic;       // 引用集合相关的命名空间（本文件中没用到，但常用于集合类型）
using System.ComponentModel;            // 引用组件模型命名空间，包含 INotifyPropertyChanged 接口
using System.Linq;                      // 引用 LINQ 相关的命名空间（本文件中没用到）
using System.Runtime.CompilerServices;  // 引用编译器服务命名空间，提供 CallerMemberName 特性
using System.Text;                      // 引用文本处理相关命名空间（本文件中没用到）
using System.Threading.Tasks;           // 引用异步和多线程相关命名空间（本文件中没用到）

namespace HostComputer.Common.Base             // 定义命名空间 HostComputer.Base，用于组织代码
{
    /// <summary>
    /// NotifyBase 类：实现 INotifyPropertyChanged 接口
    /// 用于数据绑定时，当属性值发生变化时通知 UI 更新
    /// </summary>
    public class NotifyBase : INotifyPropertyChanged
    {
        // 定义事件 PropertyChanged，当属性值变化时触发此事件
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 通知属性值已更改的方法
        /// </summary>
        /// <param name="propName">发生变化的属性名称，默认由 CallerMemberName 自动填充</param>
        public void NotifyChanged([CallerMemberName] string propName = "")
        {
            // 如果 PropertyChanged 不为空，则触发事件，并传递属性名
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }


        /// <summary>
        /// 通用 SetProperty helper：如果值变了就赋值并触发 PropertyChanged。
        /// 返回 true 表示值发生变化并已通知。
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
