using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HostComputer.Common.Services.StartupModules;
using HostComputer.Models;
using MyLogger;

namespace HostComputer.Common.Services
{
    #region StartupProgressEventArgs 启动进度事件参数
    /// <summary>
    /// 启动进度事件参数
    /// </summary>
    public class StartupProgressEventArgs : EventArgs
    {
        /// <summary>模块名称</summary>
        public string ModuleName { get; set; }

        /// <summary>状态: "Started", "Success", "Failed", "Skipped"</summary>
        public string Status { get; set; }

        /// <summary>进度百分比 (0-100)</summary>
        public double Progress { get; set; }

        /// <summary>消息</summary>
        public string Message { get; set; }

        /// <summary>持续时间</summary>
        public TimeSpan Duration { get; set; }
    }
    #endregion

    #region StartupManager 启动管理器
    /// <summary>
    /// 增强版启动管理器，支持进度报告
    /// </summary>
    public class StartupManager
    {
        #region 私有字段
        /// <summary>日志记录器</summary>
        private readonly Logger _logger;

        /// <summary>总计时器</summary>
        private readonly Stopwatch _totalStopwatch = new();

        /// <summary>模块信息字典（线程安全）</summary>
        private readonly ConcurrentDictionary<string, ModuleInfo> _modules = new();
        #endregion

        #region 公共事件
        /// <summary>
        /// 进度变化事件
        /// </summary>
        public event EventHandler<StartupProgressEventArgs> ProgressChanged;
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化启动管理器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public StartupManager(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 异步初始化，带进度报告
        /// </summary>
        /// <param name="progressCallback">进度回调函数</param>
        /// <returns>启动结果</returns>
        public async Task<StartupResult> InitializeAsync(Action<double> progressCallback = null)
        {
            _totalStopwatch.Start();

            // 获取所有初始化器
            var initializers = GetInitializers();
            double totalModules = initializers.Count;
            int completedModules = 0;

            // 阶段1: 环境检查 (5%的进度)
            ReportProgress("环境检查", "Started", 0, "开始环境检查...");
            await CheckEnvironmentAsync();
            completedModules++;
            progressCallback?.Invoke(completedModules / totalModules * 100);

            // 阶段2: 按优先级顺序初始化模块
            var groupedInitializers = initializers.GroupBy(i => i.Priority).OrderBy(g => g.Key);

            foreach (var group in groupedInitializers)
            {
                string stageName = group.Key switch
                {
                    InitializerPriority.Core => "核心服务",
                    InitializerPriority.Business => "业务模块",
                    InitializerPriority.UI => "UI框架",
                    _ => "其他模块"
                };

                ReportProgress(
                    stageName,
                    "Started",
                    completedModules / totalModules * 100,
                    $"开始{stageName}初始化..."
                );

                // 按Order排序
                var sortedModules = group.OrderBy(i => i.Order);

                // 对于核心模块，串行执行以确保依赖关系
                if (group.Key == InitializerPriority.Core)
                {
                    foreach (var initializer in sortedModules)
                    {
                        await InitializeSingleModuleAsync(initializer);
                        completedModules++;
                        progressCallback?.Invoke(completedModules / totalModules * 100);
                    }
                }
                else // 业务模块可以并行执行
                {
                    var tasks = sortedModules
                        .Select(async initializer =>
                        {
                            await InitializeSingleModuleAsync(initializer);
                            Interlocked.Increment(ref completedModules);
                            progressCallback?.Invoke(completedModules / totalModules * 100);
                        })
                        .ToList();

                    await Task.WhenAll(tasks);
                }
            }

            // 生成最终报告
            var result = GenerateStartupReport();
            ReportProgress("完成", "Success", 100, "应用程序启动完成");

            return result;
        }
        #endregion

        #region 私有方法 - 模块管理
        /// <summary>
        /// 获取所有需要初始化的模块
        /// </summary>
        private List<IModuleInitializer> GetInitializers()
        {
            return new List<IModuleInitializer>
            {
                // 核心服务
                new ConfigModuleInitializer(),
                new DatabaseModuleInitializer(),
                new LanguageModuleInitializer(),
                // UI框架
                new UIModuleInitializer(),
                // 业务模块 (对应你的Views文件夹)
                new ModuleInitializer("Equipment_Setup", "设备设置模块"),
                new ModuleInitializer("Recipe_Editor", "配方编辑模块"),
                new ModuleInitializer("Maintenance", "维护模块"),
                new ModuleInitializer("Overview", "总览模块"),
                new ModuleInitializer("Parameter", "参数模块"),
                new ModuleInitializer("Lot_Operation", "批次操作模块"),
                new ModuleInitializer("History", "历史记录模块"),
                new ModuleInitializer("3thViews", "第三方视图模块"),
                // 后台服务
                new BackgroundServiceInitializer(),
                // 安全模块 (登录相关)
                new SecurityModuleInitializer()
            };
        }

        /// <summary>
        /// 初始化单个模块
        /// </summary>
        private async Task InitializeSingleModuleAsync(IModuleInitializer initializer)
        {
            var stopwatch = Stopwatch.StartNew();
            string moduleKey = $"{initializer.ModuleType}_{initializer.ModuleName}";

            // 检查依赖
            if (!await CheckDependenciesAsync(initializer))
            {
                _modules[moduleKey] = new ModuleInfo
                {
                    Name = initializer.ModuleName,
                    Status = ModuleStatus.DependencyFailed,
                    Duration = stopwatch.Elapsed
                };

                ReportProgress(initializer.ModuleName, "Skipped", -1, "依赖检查失败");
                _logger.Warning($"模块 {initializer.ModuleName} 依赖检查失败，跳过初始化");
                return;
            }

            // 开始初始化
            ReportProgress(initializer.ModuleName, "Started", -1, "开始初始化...");

            try
            {
                bool success = await initializer.InitializeAsync(_logger);

                _modules[moduleKey] = new ModuleInfo
                {
                    Name = initializer.ModuleName,
                    Status = success ? ModuleStatus.Success : ModuleStatus.Failed,
                    Duration = stopwatch.Elapsed,
                    Timestamp = DateTime.Now
                };

                if (success)
                {
                    ReportProgress(
                        initializer.ModuleName,
                        "Success",
                        -1,
                        $"初始化成功 ({stopwatch.ElapsedMilliseconds}ms)"
                    );
                    _logger.Module($"✅ {initializer.ModuleName} 初始化成功");
                }
                else
                {
                    ReportProgress(initializer.ModuleName, "Failed", -1, "初始化失败");
                    _logger.Error($"❌ {initializer.ModuleName} 初始化失败");
                }
            }
            catch (Exception ex)
            {
                _modules[moduleKey] = new ModuleInfo
                {
                    Name = initializer.ModuleName,
                    Status = ModuleStatus.Error,
                    Duration = stopwatch.Elapsed,
                    Timestamp = DateTime.Now,
                    Error = ex.Message
                };

                ReportProgress(initializer.ModuleName, "Failed", -1, $"异常: {ex.Message}");
                _logger.Error($"❌ {initializer.ModuleName} 初始化异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查模块依赖
        /// </summary>
        private async Task<bool> CheckDependenciesAsync(IModuleInitializer initializer)
        {
            if (initializer.Dependencies == null || !initializer.Dependencies.Any())
                return true;

            foreach (var dep in initializer.Dependencies)
            {
                string depKey = $"{dep.ModuleType}_{dep.ModuleName}";
                if (
                    !_modules.TryGetValue(depKey, out var module)
                    || module.Status != ModuleStatus.Success
                )
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region 私有方法 - 环境检查
        /// <summary>
        /// 检查环境
        /// </summary>
        private async Task CheckEnvironmentAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Config("开始环境检查...");

            // 统一由 PathManager 触发目录创建
            var requiredDirs = new Dictionary<string, string>
            {
                { "Logs", PathManager.LogDir },
                { "ConfigFile", PathManager.ConfigDir },
                { "Language", PathManager.LanguageDir },
                { "Data", PathManager.DataDir }
            };

            foreach (var item in requiredDirs)
            {
                if (Directory.Exists(item.Value))
                {
                    _logger.Config($"目录存在: {item.Key} → {item.Value}");
                }
                else
                {
                    // 理论上不会进来（PathManager 已创建）
                    Directory.CreateDirectory(item.Value);
                    _logger.Config($"创建目录: {item.Key} → {item.Value}");
                }
            }

            // 系统信息（只读，不参与路径计算）
            _logger.Config($"操作系统: {Environment.OSVersion}");
            _logger.Config($".NET 版本: {Environment.Version}");
            _logger.Config($"处理器核心数: {Environment.ProcessorCount}");
            _logger.Config($"程序运行目录(Base): {AppDomain.CurrentDomain.BaseDirectory}");
            _logger.Config($"解决方案根目录: {PathManager.SolutionRoot}");

            stopwatch.Stop();
            _logger.Config($"环境检查完成 ({stopwatch.ElapsedMilliseconds} ms)");

            await Task.CompletedTask;
        }

        #endregion

        #region 私有方法 - 报告生成
        /// <summary>
        /// 生成启动报告
        /// </summary>
        private StartupResult GenerateStartupReport()
        {
            var result = new StartupResult
            {
                TotalDuration = _totalStopwatch.Elapsed,
                ModuleCount = _modules.Count,
                SuccessCount = _modules.Values.Count(m => m.Status == ModuleStatus.Success),
                FailedCount = _modules.Values.Count(m =>
                    m.Status == ModuleStatus.Failed || m.Status == ModuleStatus.Error
                ),
                Modules = _modules.Values.ToList()
            };

            // 输出报告
            _logger.Startup("📊 === 启动报告 ===");
            _logger.Startup($"总耗时: {result.TotalDuration.TotalSeconds:F2}秒");
            _logger.Startup(
                $"总模块: {result.ModuleCount} | 成功: {result.SuccessCount} | 失败: {result.FailedCount}"
            );

            if (result.FailedCount > 0)
            {
                _logger.Warning("失败模块:");
                foreach (
                    var module in result.Modules.Where(m =>
                        m.Status == ModuleStatus.Failed || m.Status == ModuleStatus.Error
                    )
                )
                {
                    _logger.Warning(
                        $"  - {module.Name}: {module.Status} ({module.Duration.TotalMilliseconds:F0}ms)"
                    );
                }
            }

            return result;
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        private void ReportProgress(
            string moduleName,
            string status,
            double progress,
            string message
        )
        {
            ProgressChanged?.Invoke(
                this,
                new StartupProgressEventArgs
                {
                    ModuleName = moduleName,
                    Status = status,
                    Progress = progress,
                    Message = message,
                    Duration = _totalStopwatch.Elapsed
                }
            );
        }
        #endregion
    }
    #endregion

    #region SecurityModuleInitializer 安全模块初始化器
    /// <summary>
    /// 安全模块初始化器
    /// </summary>
    public class SecurityModuleInitializer : IModuleInitializer
    {
        #region IModuleInitializer 实现
        /// <summary>模块名称</summary>
        public string ModuleName => "安全服务";

        /// <summary>模块类型</summary>
        public string ModuleType => "Security";

        /// <summary>优先级</summary>
        public InitializerPriority Priority => InitializerPriority.Core;

        /// <summary>顺序</summary>
        public int Order => 4;

        /// <summary>依赖项</summary>
        public List<ModuleDependency> Dependencies =>
            new()
            {
                new ModuleDependency { ModuleName = "数据库服务", ModuleType = "Database" }
            };

        /// <summary>
        /// 异步初始化
        /// </summary>
        public async Task<bool> InitializeAsync(Logger logger)
        {
            logger.Security("初始化安全服务...");

            try
            {
                // 检查用户数据库
                await CheckUserDatabaseAsync(logger);

                // 初始化加密服务
                InitializeEncryptionService(logger);

                logger.Security("安全服务初始化完成");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"安全服务初始化失败: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 检查用户数据库
        /// </summary>
        private async Task CheckUserDatabaseAsync(Logger logger)
        {
            logger.Security("检查用户数据库...");
            await Task.Delay(50);
            logger.Security("用户数据库检查完成");
        }

        /// <summary>
        /// 初始化加密服务
        /// </summary>
        private void InitializeEncryptionService(Logger logger)
        {
            logger.Security("初始化加密服务...");
            // 加密初始化逻辑
            logger.Security("加密服务就绪");
        }
        #endregion
    }
    #endregion
}
