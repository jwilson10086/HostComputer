using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Base
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PermissionAttribute : Attribute
    {
        /// <summary>
        /// 最低权限等级（0 表示不限制）
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 权限 Key（可选）
        /// </summary>
        public string? PermissionKey { get; set; }

        public PermissionAttribute(int level)
        {
            Level = level;
        }

        public PermissionAttribute()
        {
            Level = 0; // 0 = 不限制
        }
    }


}
