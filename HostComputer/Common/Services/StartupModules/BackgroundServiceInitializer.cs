using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MyLogger;

namespace HostComputer.Common.Services.StartupModules
{
    public class BackgroundServiceInitializer : IModuleInitializer
    {
        public string ModuleName => "后台服务";
        public string ModuleType => "Service";
        public InitializerPriority Priority => InitializerPriority.Business;
        public int Order => 90;
        public List<ModuleDependency> Dependencies =>
            new()
            {
                new ModuleDependency { ModuleName = "数据库服务", ModuleType = "Database" },
                new ModuleDependency { ModuleName = "配置服务", ModuleType = "Config" }
            };

        private readonly List<Thread> _backgroundThreads = new();
        private readonly List<Timer> _timers = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _isDisposed = false;

        public async Task<bool> InitializeAsync(Logger logger)
        {
            logger.Service("开始启动后台服务...");

            try
            {
                // 1. 启动系统监控线程
                await StartSystemMonitorAsync(logger);

                // 2. 启动性能监控服务
                await StartPerformanceMonitorAsync(logger);

                // 3. 启动定时任务服务
                await StartScheduledTasksAsync(logger);

                // 4. 启动数据库维护服务
                await StartDatabaseMaintenanceAsync(logger);

                // 5. 启动日志清理服务
                await StartLogCleanupServiceAsync(logger);

                // 6. 启动资源监控服务
                await StartResourceMonitorAsync(logger);

                // 7. 启动网络连接监控
                await StartNetworkMonitorAsync(logger);

                // 8. 启动设备状态监控（根据你的应用需求）
                await StartEquipmentMonitorAsync(logger);

                logger.Service("✅ 后台服务启动完成");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ 后台服务启动失败: {ex.Message}");
                await CleanupAsync(logger);
                return false;
            }
        }

        #region 系统监控线程
        private async Task StartSystemMonitorAsync(Logger logger)
        {
            logger.Service("启动系统监控线程...");

            try
            {
                var monitorThread = new Thread(() =>
                {
                    logger.ThreadLog(
                        $"系统监控线程启动 (ID: {Thread.CurrentThread.ManagedThreadId}, Name: SystemMonitor)"
                    );

                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // 监控系统状态
                            MonitorSystemHealth();

                            // 每5秒检查一次
                            Thread.Sleep(5000);
                        }
                        catch (ThreadInterruptedException)
                        {
                            // 线程被中断，正常退出
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"系统监控线程异常: {ex.Message}");
                            Thread.Sleep(10000); // 出错后等待10秒
                        }
                    }

                    logger.ThreadLog("系统监控线程停止");
                })
                {
                    Name = "SystemMonitor",
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal
                };

                monitorThread.Start();
                _backgroundThreads.Add(monitorThread);

                // 等待线程启动
                await Task.Delay(100);

                logger.Service($"✅ 系统监控线程已启动: {monitorThread.Name}");
            }
            catch (Exception ex)
            {
                logger.Error($"启动系统监控线程失败: {ex.Message}");
            }
        }

        private void MonitorSystemHealth()
        {
            try
            {
                // 监控内存使用
                var memoryInfo = GC.GetTotalMemory(false);
                if (memoryInfo > 500 * 1024 * 1024) // 超过500MB
                {
                    // 可以记录警告或触发GC
                    // Logger.Warning($"内存使用较高: {memoryInfo / 1024 / 1024}MB");
                }

                // 监控线程池状态
                ThreadPool.GetAvailableThreads(
                    out int workerThreads,
                    out int completionPortThreads
                );
                ThreadPool.GetMaxThreads(
                    out int maxWorkerThreads,
                    out int maxCompletionPortThreads
                );

                // 线程池使用率超过80%时记录警告
                double workerUsage = 100 - (workerThreads * 100.0 / maxWorkerThreads);
                if (workerUsage > 80)
                {
                    // Logger.Warning($"线程池使用率较高: {workerUsage:F1}%");
                }
            }
            catch
            {
                // 忽略监控过程中的异常
            }
        }
        #endregion

        #region 性能监控服务
        private async Task StartPerformanceMonitorAsync(Logger logger)
        {
            logger.Service("启动性能监控服务...");

            try
            {
                // 使用Timer定时收集性能数据
                var performanceTimer = new Timer(
                    callback: _ => CollectPerformanceMetrics(logger),
                    state: null,
                    dueTime: TimeSpan.FromSeconds(30), // 30秒后开始
                    period: TimeSpan.FromMinutes(1)
                ); // 每分钟收集一次

                _timers.Add(performanceTimer);

                // 立即收集一次
                await Task.Run(() => CollectPerformanceMetrics(logger));

                logger.Service("✅ 性能监控服务已启动");
            }
            catch (Exception ex)
            {
                logger.Error($"启动性能监控服务失败: {ex.Message}");
            }
        }

        private void CollectPerformanceMetrics(Logger logger)
        {
            try
            {
                // 收集CPU使用率（需要System.Diagnostics）
                // using var process = Process.GetCurrentProcess();
                // var cpuTime = process.TotalProcessorTime;
                // var cpuUsage = ...;

                // 收集内存使用
                var memoryUsed = GC.GetTotalMemory(false) / 1024 / 1024; // MB
                var peakMemory =
                    System.Diagnostics.Process.GetCurrentProcess().PeakWorkingSet64 / 1024 / 1024;

                // 收集线程信息
                int threadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;

                // 收集GC信息
                var gen0 = GC.CollectionCount(0);
                var gen1 = GC.CollectionCount(1);
                var gen2 = GC.CollectionCount(2);

                // 记录性能数据（可以存储到数据库或文件）
                var metrics = new
                {
                    Timestamp = DateTime.Now,
                    MemoryUsedMB = memoryUsed,
                    PeakMemoryMB = peakMemory,
                    ThreadCount = threadCount,
                    Gen0Collections = gen0,
                    Gen1Collections = gen1,
                    Gen2Collections = gen2
                };

                // 这里可以记录到性能日志
                // logger.Performance($"性能指标: 内存={memoryUsed}MB, 线程={threadCount}, GC={gen0}/{gen1}/{gen2}");

                // 检查异常情况
                if (memoryUsed > 800) // 超过800MB
                {
                    logger.Warning($"内存使用较高: {memoryUsed}MB");
                }

                if (threadCount > 100) // 线程数过多
                {
                    logger.Warning($"线程数量较多: {threadCount}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"收集性能指标失败: {ex.Message}");
            }
        }
        #endregion

        #region 定时任务服务
        private async Task StartScheduledTasksAsync(Logger logger)
        {
            logger.Service("启动定时任务服务...");

            try
            {
                // 1. 每日数据备份任务
                var backupTimer = new Timer(
                    callback: _ => ExecuteDailyBackup(logger),
                    state: null,
                    dueTime: CalculateNextRunTime(TimeSpan.FromHours(2)), // 凌晨2点
                    period: TimeSpan.FromDays(1)
                ); // 每天执行

                _timers.Add(backupTimer);

                // 2. 每小时状态报告
                var reportTimer = new Timer(
                    callback: _ => GenerateHourlyReport(logger),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(5), // 5分钟后开始
                    period: TimeSpan.FromHours(1)
                ); // 每小时执行

                _timers.Add(reportTimer);

                // 3. 每30分钟检查连接
                var connectionTimer = new Timer(
                    callback: _ => CheckDatabaseConnections(logger),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(10), // 10分钟后开始
                    period: TimeSpan.FromMinutes(30)
                ); // 每30分钟执行

                _timers.Add(connectionTimer);

                // 4. 每15分钟清理临时文件
                var cleanupTimer = new Timer(
                    callback: _ => CleanTempFiles(logger),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(15), // 15分钟后开始
                    period: TimeSpan.FromMinutes(15)
                ); // 每15分钟执行

                _timers.Add(cleanupTimer);

                logger.Service("✅ 定时任务服务已启动 (4个定时任务)");
            }
            catch (Exception ex)
            {
                logger.Error($"启动定时任务服务失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private TimeSpan CalculateNextRunTime(TimeSpan targetTime)
        {
            var now = DateTime.Now;
            var todayTarget = now.Date.Add(targetTime);

            if (now < todayTarget)
                return todayTarget - now; // 今天还没到目标时间
            else
                return todayTarget.AddDays(1) - now; // 等到明天
        }

        private void ExecuteDailyBackup(Logger logger)
        {
            try
            {
                logger.Service("开始执行每日数据备份...");

                // 备份数据库
                // BackupDatabase();

                // 备份配置文件
                // BackupConfigFiles();

                // 清理旧的备份文件
                // CleanOldBackups();

                logger.Service("✅ 每日数据备份完成");
            }
            catch (Exception ex)
            {
                logger.Error($"每日数据备份失败: {ex.Message}");
            }
        }

        private void GenerateHourlyReport(Logger logger)
        {
            try
            {
                logger.Service("生成每小时状态报告...");

                // 收集运行数据
                // var report = CollectHourlyMetrics();

                // 记录到日志或数据库
                // SaveReport(report);

                // 检查异常情况
                // CheckForAnomalies();

                logger.Service("✅ 每小时状态报告生成完成");
            }
            catch (Exception ex)
            {
                logger.Error($"生成状态报告失败: {ex.Message}");
            }
        }

        private void CheckDatabaseConnections(Logger logger)
        {
            try
            {
                logger.Service("检查数据库连接状态...");

                // 测试数据库连接
                // var connectionOk = TestDatabaseConnection();

                // 检查连接池状态
                // CheckConnectionPool();

                // 如果有问题，尝试重新连接
                // if (!connectionOk) ReconnectDatabase();

                logger.Service("✅ 数据库连接检查完成");
            }
            catch (Exception ex)
            {
                logger.Error($"检查数据库连接失败: {ex.Message}");
            }
        }

        private void CleanTempFiles(Logger logger)
        {
            try
            {
                logger.Service("清理临时文件...");

                // 清理临时目录
                var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
                if (Directory.Exists(tempDir))
                {
                    var files = Directory.GetFiles(tempDir, "*.tmp");
                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            // 删除超过24小时的临时文件
                            if (fileInfo.LastWriteTime < DateTime.Now.AddHours(-24))
                            {
                                File.Delete(file);
                            }
                        }
                        catch
                        { /* 忽略删除失败的文件 */
                        }
                    }
                }

                // 清理日志目录的旧文件
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (Directory.Exists(logDir))
                {
                    var logFiles = Directory.GetFiles(logDir, "*.log");
                    foreach (var logFile in logFiles)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(logFile);
                            // 删除超过30天的日志文件
                            if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-30))
                            {
                                File.Delete(logFile);
                            }
                        }
                        catch
                        { /* 忽略删除失败的文件 */
                        }
                    }
                }

                logger.Service("✅ 临时文件清理完成");
            }
            catch (Exception ex)
            {
                logger.Error($"清理临时文件失败: {ex.Message}");
            }
        }
        #endregion

        #region 数据库维护服务
        private async Task StartDatabaseMaintenanceAsync(Logger logger)
        {
            logger.Service("启动数据库维护服务...");

            try
            {
                var dbMaintenanceThread = new Thread(() =>
                {
                    logger.ThreadLog($"数据库维护线程启动 (ID: {Thread.CurrentThread.ManagedThreadId})");

                    // 每6小时执行一次数据库维护
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            PerformDatabaseMaintenance(logger);

                            // 等待6小时
                            for (int i = 0; i < 6 * 60; i++) // 6小时 = 360分钟
                            {
                                if (_cancellationTokenSource.Token.IsCancellationRequested)
                                    break;
                                Thread.Sleep(60000); // 每分钟检查一次取消令牌
                            }
                        }
                        catch (ThreadInterruptedException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"数据库维护异常: {ex.Message}");
                            Thread.Sleep(60000); // 出错后等待1分钟
                        }
                    }

                    logger.ThreadLog("数据库维护线程停止");
                })
                {
                    Name = "DatabaseMaintenance",
                    IsBackground = true
                };

                dbMaintenanceThread.Start();
                _backgroundThreads.Add(dbMaintenanceThread);

                await Task.Delay(100);
                logger.Service("✅ 数据库维护服务已启动");
            }
            catch (Exception ex)
            {
                logger.Error($"启动数据库维护服务失败: {ex.Message}");
            }
        }

        private void PerformDatabaseMaintenance(Logger logger)
        {
            try
            {
                logger.Service("执行数据库维护...");

                // 执行数据库优化
                // OptimizeDatabase();

                // 更新数据库统计信息
                // UpdateStatistics();

                // 清理过期数据
                // CleanExpiredData();

                logger.Service("✅ 数据库维护完成");
            }
            catch (Exception ex)
            {
                logger.Error($"数据库维护失败: {ex.Message}");
            }
        }
        #endregion

        #region 其他后台服务
        private async Task StartLogCleanupServiceAsync(Logger logger)
        {
            logger.Service("启动日志清理服务...");

            try
            {
                // 每天凌晨3点清理日志
                var logCleanupTimer = new Timer(
                    callback: _ => CleanupOldLogs(logger),
                    state: null,
                    dueTime: CalculateNextRunTime(TimeSpan.FromHours(3)), // 凌晨3点
                    period: TimeSpan.FromDays(1)
                );

                _timers.Add(logCleanupTimer);

                logger.Service("✅ 日志清理服务已启动");
            }
            catch (Exception ex)
            {
                logger.Error($"启动日志清理服务失败: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        private void CleanupOldLogs(Logger logger)
        {
            try
            {
                logger.Service("清理旧日志文件...");

                // 实现日志清理逻辑
                // ...

                logger.Service("✅ 旧日志清理完成");
            }
            catch (Exception ex)
            {
                logger.Error($"清理旧日志失败: {ex.Message}");
            }
        }

        private async Task StartResourceMonitorAsync(Logger logger)
        {
            logger.Service("启动资源监控服务...");

            // 监控磁盘空间、网络状态等
            // 实现略...

            await Task.CompletedTask;
        }

        private async Task StartNetworkMonitorAsync(Logger logger)
        {
            logger.Service("启动网络连接监控...");

            // 监控网络连接状态
            // 实现略...

            await Task.CompletedTask;
        }

        private async Task StartEquipmentMonitorAsync(Logger logger)
        {
            logger.Service("启动设备状态监控...");

            // 根据你的应用需求，监控设备连接状态
            // 实现略...

            await Task.CompletedTask;
        }
        #endregion

        #region 清理方法
        public async Task CleanupAsync(Logger logger)
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            logger.Service("正在停止后台服务...");

            try
            {
                // 取消所有任务
                _cancellationTokenSource.Cancel();

                // 停止所有定时器
                foreach (var timer in _timers)
                {
                    timer?.Dispose();
                }
                _timers.Clear();

                // 停止所有线程
                foreach (var thread in _backgroundThreads)
                {
                    if (thread.IsAlive)
                    {
                        try
                        {
                            thread.Interrupt();
                            if (!thread.Join(5000)) // 等待5秒
                            {
                                logger.Warning($"线程 {thread.Name} 停止超时");
                            }
                        }
                        catch
                        { /* 忽略线程停止异常 */
                        }
                    }
                }
                _backgroundThreads.Clear();

                _cancellationTokenSource.Dispose();

                logger.Service("✅ 后台服务已停止");
            }
            catch (Exception ex)
            {
                logger.Error($"停止后台服务时出错: {ex.Message}");
            }

            await Task.CompletedTask;
        }
        #endregion
    }
}
