using System;
using System.Collections.Concurrent;

namespace HostComputer.Common.Actions
{
    #region ActionManager 全局行为管理器
    /// <summary>
    /// 全局行为管理器（工业组态 / UI 解耦专用）
    /// 提供线程安全的全局行为注册和执行机制
    /// </summary>
    public static class ActionManager
    {
        #region 私有字段
        /// <summary>
        /// 行为字典（线程安全）
        /// </summary>
        private static readonly ConcurrentDictionary<string, Delegate> _actions
            = new ConcurrentDictionary<string, Delegate>();
        #endregion

        #region 注册方法
        /// <summary>
        /// 注册无返回值行为
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">行为唯一标识</param>
        /// <param name="action">要注册的行为</param>
        /// <param name="overwrite">是否覆盖已存在的注册</param>
        /// <returns>是否注册成功</returns>
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
        /// <typeparam name="TIn">输入参数类型</typeparam>
        /// <typeparam name="TOut">返回值类型</typeparam>
        /// <param name="key">行为唯一标识</param>
        /// <param name="func">要注册的函数</param>
        /// <param name="overwrite">是否覆盖已存在的注册</param>
        /// <returns>是否注册成功</returns>
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

        #region 执行方法
        /// <summary>
        /// 执行无返回值行为
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">行为唯一标识</param>
        /// <param name="param">执行参数</param>
        /// <returns>是否执行成功</returns>
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
        /// <typeparam name="TIn">输入参数类型</typeparam>
        /// <typeparam name="TOut">返回值类型</typeparam>
        /// <param name="key">行为唯一标识</param>
        /// <param name="param">执行参数</param>
        /// <param name="result">执行结果</param>
        /// <returns>是否执行成功</returns>
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
        /// <typeparam name="TIn">输入参数类型</typeparam>
        /// <typeparam name="TOut">返回值类型</typeparam>
        /// <param name="key">行为唯一标识</param>
        /// <param name="param">执行参数</param>
        /// <returns>执行结果，如果行为未注册返回默认值</returns>
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

        #region 管理方法
        /// <summary>
        /// 注销行为
        /// </summary>
        /// <param name="key">行为唯一标识</param>
        /// <returns>是否注销成功</returns>
        public static bool Unregister(string key)
        {
            return _actions.TryRemove(key, out _);
        }

        /// <summary>
        /// 检查行为是否已注册
        /// </summary>
        /// <param name="key">行为唯一标识</param>
        /// <returns>是否已注册</returns>
        public static bool Contains(string key)
        {
            return _actions.ContainsKey(key);
        }

        /// <summary>
        /// 清空所有注册的行为（慎用）
        /// </summary>
        public static void Clear()
        {
            _actions.Clear();
        }
        #endregion
    }
    #endregion
}