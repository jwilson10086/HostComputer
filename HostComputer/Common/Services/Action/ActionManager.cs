using System;
using System.Collections.Concurrent;

namespace HostComputer.Common.Action
{
    /// <summary>
    /// 全局行为管理器（工业组态 / UI 解耦专用）
    /// </summary>
    public static class ActionManager
    {
        /// <summary>
        /// 行为字典（线程安全）
        /// </summary>
        private static readonly ConcurrentDictionary<string, Delegate> _actions
            = new ConcurrentDictionary<string, Delegate>();

        #region Register

        /// <summary>
        /// 注册无返回值行为
        /// </summary>
        public static bool Register<T>(string key, Action<T> action, bool overwrite = false)
        {
            if (overwrite)
            {
                _actions[key] = action;
                return true;
            }

            return _actions.TryAdd(key, action);
        }

        /// <summary>
        /// 注册有返回值行为
        /// </summary>
        public static bool Register<TIn, TOut>(
            string key,
            Func<TIn, TOut> func,
            bool overwrite = false)
        {
            if (overwrite)
            {
                _actions[key] = func;
                return true;
            }

            return _actions.TryAdd(key, func);
        }

        #endregion

        #region Execute

        /// <summary>
        /// 执行无返回值行为
        /// </summary>
        public static bool Execute<T>(string key, T param)
        {
            if (_actions.TryGetValue(key, out var d) &&
                d is Action<T> action)
            {
                action(param);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行有返回值行为
        /// </summary>
        public static bool Execute<TIn, TOut>(
            string key,
            TIn param,
            out TOut? result)
        {
            result = default;

            if (_actions.TryGetValue(key, out var d) &&
                d is Func<TIn, TOut> func)
            {
                result = func(param);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行有返回值行为（简化版）
        /// </summary>
        public static TOut? Execute<TIn, TOut>(string key, TIn param)
        {
            if (_actions.TryGetValue(key, out var d) &&
                d is Func<TIn, TOut> func)
            {
                return func(param);
            }

            return default;
        }

        #endregion

        #region Manage

        /// <summary>
        /// 注销行为
        /// </summary>
        public static bool Unregister(string key)
        {
            return _actions.TryRemove(key, out _);
        }

        /// <summary>
        /// 是否已注册
        /// </summary>
        public static bool Contains(string key)
        {
            return _actions.ContainsKey(key);
        }

        /// <summary>
        /// 清空（慎用）
        /// </summary>
        public static void Clear()
        {
            _actions.Clear();
        }

        #endregion
    }
}
