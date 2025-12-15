namespace MyLogger
{
    /// <summary>
    /// 日志等级枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,    // 调试信息，详细信息
        Info,     // 一般信息
        Warning,  // 警告信息
        Error,    // 错误信息
        Fatal     // 致命错误
    }
    public enum LogCategory
    {
        Startup,    // 应用启动/关闭
        Module,     // 模块加载
        Thread,     // 线程管理
        Service,    // 服务启动
        Database,   // 数据库操作
        UI,         // UI相关
        Config,     // 配置加载
        Security,   // 安全/登录
        Business,   // 业务逻辑
        System      // 系统事件
    }

}
