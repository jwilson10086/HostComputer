using System;
using System.Collections.Concurrent; // 支持线程安全的阻塞集合
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MyLogger
{
    /// <summary>
    /// 日志记录器类，支持异步写日志、控制台输出，并自动处理程序退出时剩余日志
    /// </summary>
    /// 
   
    public class Logger : IDisposable
    {
        private readonly LoggerConfig _config;                 // 日志配置对象
        private readonly BlockingCollection<string> _logQueue = new(); // 日志队列，线程安全，可阻塞
        private readonly Task _logTask;                        // 后台写日志任务
        private bool _disposed = false;                        // 防止重复调用 Dispose

        /// <summary>
        /// 构造函数，初始化日志记录器
        /// </summary>
        /// <param name="config">LoggerConfig 对象，配置日志目录、文件名、最小日志等级等</param>
        public Logger(LoggerConfig config)
        {
            _config = config;

            // 如果日志目录不存在，则创建
            if (!Directory.Exists(_config.LogDirectory))
                Directory.CreateDirectory(_config.LogDirectory);

            // 启动后台任务，处理日志队列
            _logTask = Task.Factory.StartNew(ProcessLogQueue, TaskCreationOptions.LongRunning);

            // 注册程序退出事件，确保退出前写完剩余日志
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }
        
            public void Startup(string msg) => Log(LogCategory.Startup, msg);
            public void Module(string msg) => Log(LogCategory.Module, msg);
            public void ThreadLog(string msg) => Log(LogCategory.Thread, msg);
            public void Service(string msg) => Log(LogCategory.Service, msg);
            public void Database(string msg) => Log(LogCategory.Database, msg);
            public void UI(string msg) => Log(LogCategory.UI, msg);
            public void Config(string msg) => Log(LogCategory.Config, msg);
            public void Security(string msg) => Log(LogCategory.Security, msg);

            // 扩展控制台颜色
            private void WriteLogToConsole(string log)
            {
                ConsoleColor color = DetermineLogColor(log);
                Console.ForegroundColor = color;
                Console.Write(log);
                Console.ResetColor();
            }

            private ConsoleColor DetermineLogColor(string log)
            {
                return log switch
                {
                    string s when s.Contains("[Startup]") => ConsoleColor.Cyan,
                    string s when s.Contains("[Module]") => ConsoleColor.Blue,
                    string s when s.Contains("[Thread]") => ConsoleColor.Magenta,
                    string s when s.Contains("[Service]") => ConsoleColor.Green,
                    string s when s.Contains("[Database]") => ConsoleColor.Yellow,
                    string s when s.Contains("[UI]") => ConsoleColor.White,
                    string s when s.Contains("[Config]") => ConsoleColor.DarkCyan,
                    string s when s.Contains("[Security]") => ConsoleColor.DarkYellow,
                    string s when s.Contains("[Debug]") => ConsoleColor.Gray,
                    string s when s.Contains("[Info]") => ConsoleColor.Green,
                    string s when s.Contains("[Warning]") => ConsoleColor.Yellow,
                    string s when s.Contains("[Error]") => ConsoleColor.Red,
                    string s when s.Contains("[Fatal]") => ConsoleColor.DarkRed,
                    _ => ConsoleColor.White
                };
            }
            /// <summary>
            /// 后台线程循环处理日志队列
            /// </summary>
            private void ProcessLogQueue()
        {
            // GetConsumingEnumerable 会阻塞等待新日志，同时在 CompleteAdding 后结束循环
            foreach (var log in _logQueue.GetConsumingEnumerable())
            {
                WriteLogToFile(log); // 写入日志文件

                // 如果启用了控制台输出，则输出
                if (_config.EnableConsole)
                    WriteLogToConsole(log);

            }
        }

        /// <summary>
        /// 将日志写入文件，按日期生成文件名，超过大小自动生成新文件
        /// </summary>
        /// <param name="log">日志内容</param>
        private void WriteLogToFile(string log)
        {
            string filePath = Path.Combine(_config.LogDirectory,
                $"{DateTime.Now:yyyyMMdd}_{_config.LogFileName}");

            // 判断文件是否超过最大大小，如果超过，生成带时间戳的新文件
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > _config.MaxFileSizeMB * 1024 * 1024)
                {
                    filePath = Path.Combine(_config.LogDirectory,
                        $"{DateTime.Now:yyyyMMdd_HHmmss}_{_config.LogFileName}");
                }
            }

            // 尝试写入文件，最多重试3次
            int maxRetries = 3;
            int retryDelayMs = 50; // 每次重试前等待的毫秒数

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // 关键修改：使用 FileShare.ReadWrite 打开文件，允许其他进程读取或写入
                    using (var stream = new FileStream(filePath,
                                                      FileMode.Append,
                                                      FileAccess.Write,
                                                      FileShare.ReadWrite)) // 允许共享读写
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.Write(log);
                    }
                    // 写入成功，退出循环
                    return;
                }
                catch (IOException) when (attempt < maxRetries) // 捕获IO异常且未达到最大重试次数
                {
                    // 等待一小段时间后重试
                    Thread.Sleep(retryDelayMs);
                    Console.WriteLine($"警告: 无法写入日志文件 '{filePath}'。 尝试重试中...");
                }
                catch (IOException ex) // 重试多次后仍然失败
                {
                    // 这里可以选择将错误输出到控制台，避免再次递归调用日志记录
                    Console.Error.WriteLine($"严重: 无法写入日志文件 '{filePath}'。 错误: {ex.Message}");
                    // 注意：这里选择静默失败，避免程序因日志问题而崩溃
                    return;
                }
            }
        }

        /// <summary>
        /// 内部日志方法，根据等级决定是否记录
        /// </summary>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志内容</param>
        private void Log(LogLevel level, string message)
        {
            if (level < _config.MinLogLevel) return;

            string log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";

            try
            {
                _logQueue.Add(log);
            }
            catch (InvalidOperationException)
            {
                // 队列已关闭，静默失败或输出到控制台
                if (_config.EnableConsole)
                {
                    WriteLogToConsole(log);
                }
            }
        }

        private void Log(LogCategory category, string message)
        {
            
            string log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{category}] {message}{Environment.NewLine}";
            _logQueue.Add(log);
        }

        // 对外提供的日志方法，根据等级调用内部 Log 方法
        public void Debug(string msg) => Log(LogLevel.Debug, msg);
        public void Info(string msg) => Log(LogLevel.Info, msg);
        public void Warning(string msg) => Log(LogLevel.Warning, msg);
        public void Error(string msg) => Log(LogLevel.Error, msg);
        public void Error(string msg, Exception ex) => Log(LogLevel.Error, $"{msg} Exception: {ex}");
        public void Fatal(string msg) => Log(LogLevel.Fatal, msg);




        /// <summary>
        /// Dispose 方法，释放资源
        /// </summary>
        public void Dispose()
        {
            // 防止重复释放
            if (_disposed) return;
            _disposed = true;

            // 通知队列不再添加新日志
            _logQueue.CompleteAdding();

            // 等待后台写日志任务完成
            _logTask.Wait();

            // 注销程序退出事件，避免多次触发 Dispose
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        }

        /// <summary>
        /// 程序退出事件处理，确保退出前写完日志
        /// </summary>
        private void OnProcessExit(object sender, EventArgs e)
        {
            Dispose(); // 调用 Dispose 确保日志处理完成
        }
    }
}
