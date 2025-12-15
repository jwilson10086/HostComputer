using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.Common.Services
{
    public enum SystemState
    {
        Offline,
        Online,
        Simulation,
        Error
    }

    /// <summary>
    /// 系统状态机管理
    /// 全局单例
    /// </summary>
    public class SystemStateManager
    {
        private static readonly Lazy<SystemStateManager> _instance = new Lazy<SystemStateManager>(
            () => new SystemStateManager()
        );

        public static SystemStateManager Instance => _instance.Value;

        private SystemStateManager() { }

        public SystemState CurrentState { get; private set; } = SystemState.Offline;

        /// <summary>
        /// 状态变化事件，所有模块订阅
        /// </summary>
        public event Action<SystemState> OnStateChanged;

        /// <summary>
        /// 尝试切换状态
        /// </summary>
        public bool SetState(SystemState newState)
        {
            if (!CanTransition(CurrentState, newState))
                return false;

            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            return true;
        }

        /// <summary>
        /// 状态跳转合法性判断
        /// </summary>
        private bool CanTransition(SystemState from, SystemState to)
        {
            return (from, to) switch
            {
                (SystemState.Offline, SystemState.Online) => true,
                (SystemState.Offline, SystemState.Simulation) => true,
                (SystemState.Online, SystemState.Error) => true,
                (SystemState.Simulation, SystemState.Error) => true,
                (SystemState.Error, SystemState.Offline) => true,
                _ => false
            };
        }
    }
}
