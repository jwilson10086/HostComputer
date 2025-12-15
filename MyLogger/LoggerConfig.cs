namespace MyLogger
{
    /// <summary>
    /// 日志配置类
    /// </summary>
    public class LoggerConfig
    {
        /// <summary>
        /// 日志文件存放目录
        /// </summary>
        public string LogDirectory { get; set; } = "Logs";

        /// <summary>
        /// 日志文件名，最终会自动加日期前缀
        /// </summary>
        public string LogFileName { get; set; } = "app.log";

        /// <summary>
        /// 是否在控制台输出日志
        /// </summary>
        public bool EnableConsole { get; set; } = true;

        /// <summary>
        /// 最低日志等级，低于此等级的日志不会被记录
        /// </summary>
        public LogLevel MinLogLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// 单个日志文件最大大小（MB），超过会自动新建文件
        /// </summary>
        public long MaxFileSizeMB { get; set; } = 5;
    }
}
