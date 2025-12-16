using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using MyLogger;

namespace HostComputer.Common.Services.StartupModules
{
    #region AppConfig 应用配置类
    /// <summary>
    /// 应用程序配置类
    /// </summary>
    public class AppConfig
    {
        /// <summary>环境名称</summary>
        public string Environment { get; set; } = "Development";

        /// <summary>数据库配置</summary>
        public DatabaseConfig Database { get; set; } = new();

        /// <summary>日志配置</summary>
        public LoggerConfig LoggingConfig { get; set; } =
            new LoggerConfig
            {
                EnableConsole = true,
                LogDirectory = "Logs",
                MinLogLevel = LogLevel.Debug,
                LogFileName = "app.log",
                MaxFileSizeMB = 10
            };

        /// <summary>用户界面配置</summary>
        public UIConfig UI { get; set; } = new();

        /// <summary>安全配置</summary>
        public SecurityConfig Security { get; set; } = new();

        #region 嵌套配置类
        /// <summary>
        /// 数据库配置
        /// </summary>
        public class DatabaseConfig
        {
            /// <summary>数据库连接字符串</summary>
            public string ConnectionString { get; set; } = "Data Source=Data/app.db";

            /// <summary>命令超时时间（秒）</summary>
            public int CommandTimeout { get; set; } = 30;

            /// <summary>是否启用外键约束</summary>
            public bool EnableForeignKeys { get; set; } = true;
        }

        /// <summary>
        /// 用户界面配置
        /// </summary>
        public class UIConfig
        {
            /// <summary>全局字体大小</summary>
            public double FontSize { get; set; } = 14;

            /// <summary>全局主题（Light / Dark）</summary>
            public string Theme { get; set; } = "Light";

            /// <summary>默认窗口宽度</summary>
            public double WindowWidth { get; set; } = 1200;

            /// <summary>默认窗口高度</summary>
            public double WindowHeight { get; set; } = 800;

            /// <summary>是否显示窗口标题栏</summary>
            public bool ShowTitleBar { get; set; } = true;

            /// <summary>控件默认圆角</summary>
            public double CornerRadius { get; set; } = 4;

            /// <summary>语言设置（如 "en-US", "zh-CN"）</summary>
            public string Language { get; set; } = "zh-CN";
        }

        /// <summary>
        /// 安全配置
        /// </summary>
        public class SecurityConfig
        {
            /// <summary>是否启用自动登录</summary>
            public bool EnableAutoLogin { get; set; } = false;

            /// <summary>会话超时时间（分钟）</summary>
            public int SessionTimeout { get; set; } = 30;

            /// <summary>是否需要强密码</summary>
            public bool RequireStrongPassword { get; set; } = false;
        }
        #endregion
    }
    #endregion

    #region AppConfiguration 全局配置访问类
    /// <summary>
    /// 全局配置访问类
    /// </summary>
    public static class AppConfiguration
    {
        #region 公共属性
        /// <summary>
        /// 当前应用程序配置
        /// </summary>
        public static AppConfig Current { get; set; } = new AppConfig();
        #endregion

        #region 公共事件
        /// <summary>
        /// 配置变更事件
        /// </summary>
        public static event Action? OnConfigChanged;
        #endregion

        #region 公共方法
        /// <summary>
        /// 触发配置变更事件（会尽量在 UI 线程执行）
        /// </summary>
        public static void RaiseConfigChanged()
        {
            var handlers = OnConfigChanged;
            if (handlers == null)
                return;

            try
            {
                var app = Application.Current;
                if (app != null && app.Dispatcher != null && !app.Dispatcher.CheckAccess())
                {
                    app.Dispatcher.Invoke(() => handlers.Invoke());
                }
                else
                {
                    handlers.Invoke();
                }
            }
            catch
            {
                try
                {
                    handlers.Invoke();
                }
                catch { }
            }
        }
        #endregion
    }
    #endregion

    #region ConfigModuleInitializer 配置模块初始化器
    /// <summary>
    /// 配置模块初始化器
    /// </summary>
    public class ConfigModuleInitializer : IModuleInitializer
    {
        #region IModuleInitializer 实现
        /// <summary>模块名称</summary>
        public string ModuleName => "配置服务";

        /// <summary>模块类型</summary>
        public string ModuleType => "Config";

        /// <summary>优先级</summary>
        public InitializerPriority Priority => InitializerPriority.Core;

        /// <summary>顺序</summary>
        public int Order => 1;

        /// <summary>依赖项</summary>
        public List<ModuleDependency> Dependencies => new();
        #endregion

        #region 私有字段
        /// <summary>配置文件目录</summary>
        private readonly string configDir = "Config";

        /// <summary>主配置文件路径</summary>
        private readonly string mainConfigFile = "Config/appsettings.json";

        /// <summary>环境配置文件路径</summary>
        private readonly string envConfigFile = "Config/appsettings.Production.json";

        /// <summary>文件系统监视器</summary>
        private FileSystemWatcher watcher;
        #endregion

        #region 公共方法
        /// <summary>
        /// 异步初始化配置服务
        /// </summary>
        public async Task<bool> InitializeAsync(Logger logger)
        {
            logger.Config("开始加载应用程序配置...");

            try
            {
                await EnsureConfigDirectory(logger);

                var config = await LoadAndMergeConfigs(logger);

                bool valid = await ValidateConfigAsync(config, logger);
                if (!valid)
                {
                    logger.Warning("配置错误，自动使用默认配置");
                    config = await CreateDefaultConfigAsync(logger);
                }

                RegisterToGlobal(config, logger);

                await SaveIfNotExist(config, logger);

                SetupConfigWatcher(logger);
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    (App.Current as App)?.ApplyUIConfig();
                });
                logger.Config("✅ 配置服务初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ 配置初始化失败: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 私有方法 - 配置文件处理
        /// <summary>
        /// 确保配置目录存在
        /// </summary>
        private async Task EnsureConfigDirectory(Logger logger)
        {
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
                Directory.CreateDirectory(Path.Combine(configDir, "Backups"));
                logger.Config("已创建 Config 文件夹");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 加载和合并配置文件
        /// </summary>
        private async Task<AppConfig> LoadAndMergeConfigs(Logger logger)
        {
            AppConfig config = await LoadOneConfig(mainConfigFile, logger) ?? new AppConfig();

            if (File.Exists(envConfigFile))
            {
                logger.Config("加载环境配置: Production");

                AppConfig envConfig = await LoadOneConfig(envConfigFile, logger);

                if (envConfig != null)
                    MergeConfig(config, envConfig);
            }

            return config;
        }

        /// <summary>
        /// 加载单个配置文件
        /// </summary>
        private async Task<AppConfig?> LoadOneConfig(string file, Logger logger)
        {
            try
            {
                if (!File.Exists(file))
                    return null;

                string json = await File.ReadAllTextAsync(file);
                return JsonSerializer.Deserialize<AppConfig>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true,
                        Converters = { new JsonStringEnumConverter() }
                    }
                );
            }
            catch (JsonException)
            {
                await BackupCorruptedConfigAsync(file, logger);
                return null;
            }
        }

        /// <summary>
        /// 合并配置（覆盖非空字段）
        /// </summary>
        private void MergeConfig(AppConfig baseConfig, AppConfig overrideConfig)
        {
            // 只覆盖非空字段，确保灵活
            baseConfig.UI.Theme = overrideConfig.UI.Theme ?? baseConfig.UI.Theme;
            baseConfig.UI.Language = overrideConfig.UI.Language ?? baseConfig.UI.Language;

            baseConfig.Database.ConnectionString =
                overrideConfig.Database.ConnectionString ?? baseConfig.Database.ConnectionString;
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private async Task<bool> ValidateConfigAsync(AppConfig config, Logger logger)
        {
            logger.Config("验证配置...");

            bool ok = true;

            if (string.IsNullOrWhiteSpace(config.Database.ConnectionString))
            {
                ok = false;
                logger.Error("数据库连接字符串不能为空");
            }

            if (config.UI.FontSize < 8 || config.UI.FontSize > 32)
            {
                logger.Warning($"字体大小 {config.UI.FontSize} 不合理 → 自动修复为 14");
                config.UI.FontSize = 14;
            }

            await Task.Delay(30);
            return ok;
        }

        /// <summary>
        /// 注册为全局可访问配置
        /// </summary>
        private void RegisterToGlobal(AppConfig config, Logger logger)
        {
            AppConfiguration.Current = config;

            Application.Current.Resources["AppConfig"] = config;

            logger.Config($"主题：{config.UI.Theme}");
            logger.Config($"语言：{config.UI.Language}");
        }

        /// <summary>
        /// 如果主配置不存在则保存
        /// </summary>
        private async Task SaveIfNotExist(AppConfig config, Logger logger)
        {
            if (!File.Exists(mainConfigFile))
            {
                var json = JsonSerializer.Serialize(
                    config,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                await File.WriteAllTextAsync(mainConfigFile, json);
                logger.Config($"默认配置已生成: {mainConfigFile}");
            }
        }

        /// <summary>
        /// 备份损坏的配置文件
        /// </summary>
        private async Task BackupCorruptedConfigAsync(string file, Logger logger)
        {
            string backup =
                $"Config/Backups/{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            File.Copy(file, backup, true);
            logger.Warning($"已备份损坏的配置文件 → {backup}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private async Task<AppConfig> CreateDefaultConfigAsync(Logger logger)
        {
            logger.Config("创建默认配置...");

            var defaultConfig = new AppConfig
            {
                Environment = "Development",
                Database = new AppConfig.DatabaseConfig
                {
                    ConnectionString = "Data Source=Data/app.db",
                    CommandTimeout = 30,
                    EnableForeignKeys = true
                },

                UI = new AppConfig.UIConfig
                {
                    FontSize = 14,
                    Theme = "Light",
                    WindowWidth = 1200,
                    WindowHeight = 800,
                    ShowTitleBar = true,
                    CornerRadius = 4,
                    Language = "zh-CN"
                },
                Security = new AppConfig.SecurityConfig
                {
                    EnableAutoLogin = false,
                    SessionTimeout = 30,
                    RequireStrongPassword = false
                }
            };

            // 将默认配置写到主配置文件（覆盖或创建）
            try
            {
                var json = JsonSerializer.Serialize(
                    defaultConfig,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                await File.WriteAllTextAsync(mainConfigFile, json);
                logger.Config($"已将默认配置写入: {mainConfigFile}");
            }
            catch (Exception ex)
            {
                logger.Error($"写入默认配置失败: {ex.Message}");
            }

            // 注册并触发一次事件，通知订阅方
            RegisterToGlobal(defaultConfig, logger);
            AppConfiguration.RaiseConfigChanged();

            await Task.Delay(20);
            logger.Config("✅ 默认配置创建完成");
            return defaultConfig;
        }
        #endregion

        #region 私有方法 - 配置热更新
        /// <summary>
        /// 设置配置监视器（热更新）
        /// </summary>
        private void SetupConfigWatcher(Logger logger)
        {
            watcher = new FileSystemWatcher(configDir, "*.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Changed += async (_, e) =>
            {
                try
                {
                    await Task.Delay(200); // 防止文件锁
                    var config = await LoadAndMergeConfigs(logger);
                    RegisterToGlobal(config, logger);

                    logger.Config($"🔄 配置已重新加载（{e.Name}）");

                    // 安全触发：使用 AppConfiguration.RaiseConfigChanged()
                    AppConfiguration.RaiseConfigChanged();
                }
                catch (Exception ex)
                {
                    logger.Error("热载入配置失败：" + ex.Message);
                }
            };

            watcher.EnableRaisingEvents = true;
            logger.Config("已启动配置热更新监听");
        }
        #endregion
    }
    #endregion
}