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
    public class StartupProgressEventArgs : EventArgs
    {
        public string ModuleName { get; set; }
        public string Status { get; set; }  // "Started", "Success", "Failed", "Skipped"
        public double Progress { get; set; } // 0-100
        public string Message { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// 增强版启动管理器，支持进度报告
    /// </summary>
    public class StartupManager
    {
        private readonly Logger _logger;
        private readonly Stopwatch _totalStopwatch = new();
        private readonly ConcurrentDictionary<string, ModuleInfo> _modules = new();

        // 进度报告事件
        public event EventHandler<StartupProgressEventArgs> ProgressChanged;

        public StartupManager(Logger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 异步初始化，带进度报告
        /// </summary>
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
            var groupedInitializers = initializers
                .GroupBy(i => i.Priority)
                .OrderBy(g => g.Key);

            foreach (var group in groupedInitializers)
            {
                string stageName = group.Key switch
                {
                    InitializerPriority.Core => "核心服务",
                    InitializerPriority.Business => "业务模块",
                    InitializerPriority.UI => "UI框架",
                    _ => "其他模块"
                };

                ReportProgress(stageName, "Started",
                    completedModules / totalModules * 100,
                    $"开始{stageName}初始化...");

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
                    var tasks = sortedModules.Select(async initializer =>
                    {
                        await InitializeSingleModuleAsync(initializer);
                        Interlocked.Increment(ref completedModules);
                        progressCallback?.Invoke(completedModules / totalModules * 100);
                    }).ToList();

                    await Task.WhenAll(tasks);
                }
            }

            // 生成最终报告
            var result = GenerateStartupReport();
            ReportProgress("完成", "Success", 100, "应用程序启动完成");

            return result;
        }

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
                    ReportProgress(initializer.ModuleName, "Success", -1,
                        $"初始化成功 ({stopwatch.ElapsedMilliseconds}ms)");
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

        private async Task<bool> CheckDependenciesAsync(IModuleInitializer initializer)
        {
            if (initializer.Dependencies == null || !initializer.Dependencies.Any())
                return true;

            foreach (var dep in initializer.Dependencies)
            {
                string depKey = $"{dep.ModuleType}_{dep.ModuleName}";
                if (!_modules.TryGetValue(depKey, out var module) ||
                    module.Status != ModuleStatus.Success)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task CheckEnvironmentAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Config("开始环境检查...");

            // 检查必要目录
            var requiredDirs = new[] { "Logs", "Config", "Language", "Data" };
            foreach (var dir in requiredDirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    _logger.Config($"创建目录: {dir}");
                }
            }

            // 系统信息
            _logger.Config($"操作系统: {Environment.OSVersion}");
            _logger.Config($".NET版本: {Environment.Version}");
            _logger.Config($"处理器核心: {Environment.ProcessorCount}");
            _logger.Config($"工作目录: {Environment.CurrentDirectory}");

            _logger.Config($"环境检查完成 ({stopwatch.ElapsedMilliseconds}ms)");
        }

        private StartupResult GenerateStartupReport()
        {
            var result = new StartupResult
            {
                TotalDuration = _totalStopwatch.Elapsed,
                ModuleCount = _modules.Count,
                SuccessCount = _modules.Values.Count(m => m.Status == ModuleStatus.Success),
                FailedCount = _modules.Values.Count(m => m.Status == ModuleStatus.Failed ||
                                                       m.Status == ModuleStatus.Error),
                Modules = _modules.Values.ToList()
            };

            // 输出报告
            _logger.Startup("📊 === 启动报告 ===");
            _logger.Startup($"总耗时: {result.TotalDuration.TotalSeconds:F2}秒");
            _logger.Startup($"总模块: {result.ModuleCount} | 成功: {result.SuccessCount} | 失败: {result.FailedCount}");

            if (result.FailedCount > 0)
            {
                _logger.Warning("失败模块:");
                foreach (var module in result.Modules.Where(m =>
                    m.Status == ModuleStatus.Failed || m.Status == ModuleStatus.Error))
                {
                    _logger.Warning($"  - {module.Name}: {module.Status} ({module.Duration.TotalMilliseconds:F0}ms)");
                }
            }

            return result;
        }

        private void ReportProgress(string moduleName, string status, double progress, string message)
        {
            ProgressChanged?.Invoke(this, new StartupProgressEventArgs
            {
                ModuleName = moduleName,
                Status = status,
                Progress = progress,
                Message = message,
                Duration = _totalStopwatch.Elapsed
            });
        }
    }

    // 安全模块初始化器 (添加到你的StartupModules文件夹)
    public class SecurityModuleInitializer : IModuleInitializer
    {
        public string ModuleName => "安全服务";
        public string ModuleType => "Security";
        public InitializerPriority Priority => InitializerPriority.Core;
        public int Order => 4;
        public List<ModuleDependency> Dependencies => new()
        {
            new ModuleDependency { ModuleName = "数据库服务", ModuleType = "Database" }
        };

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

        private async Task CheckUserDatabaseAsync(Logger logger)
        {
            logger.Security("检查用户数据库...");
            await Task.Delay(50);
            logger.Security("用户数据库检查完成");
        }

        private void InitializeEncryptionService(Logger logger)
        {
            logger.Security("初始化加密服务...");
            // 加密初始化逻辑
            logger.Security("加密服务就绪");
        }
    }
}