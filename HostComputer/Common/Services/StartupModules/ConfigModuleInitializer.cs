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
    public class AppConfig
    {
        public string Environment { get; set; } = "Development"; // 新增: 环境名称
        public DatabaseConfig Database { get; set; } = new();
        public LoggerConfig LoggingConfig { get; set; } =
            new LoggerConfig
            {
                EnableConsole = true,
                LogDirectory = "Logs",
                MinLogLevel = LogLevel.Debug,
                LogFileName = "app.log",
                MaxFileSizeMB = 10
            };
        public UIConfig UI { get; set; } = new();
        public SecurityConfig Security { get; set; } = new();

        public class DatabaseConfig
        {
            public string ConnectionString { get; set; } = "Data Source=Data/app.db";
            public int CommandTimeout { get; set; } = 30;
            public bool EnableForeignKeys { get; set; } = true;
        }

        public class UIConfig
        {
            /// <summary>
            /// 全局字体大小
            /// </summary>
            public double FontSize { get; set; } = 14;

            /// <summary>
            /// 全局主题（Light / Dark）
            /// </summary>
            public string Theme { get; set; } = "Light";

            /// <summary>
            /// 默认窗口宽度
            /// </summary>
            public double WindowWidth { get; set; } = 1200;

            /// <summary>
            /// 默认窗口高度
            /// </summary>
            public double WindowHeight { get; set; } = 800;

            /// <summary>
            /// 是否显示窗口标题栏
            /// </summary>
            public bool ShowTitleBar { get; set; } = true;

            /// <summary>
            /// 控件默认圆角
            /// </summary>
            public double CornerRadius { get; set; } = 4;

            /// <summary>
            /// 语言设置（如 "en-US", "zh-CN"）
            /// </summary>
            public string Language { get; set; } = "zh-CN";
        }

        public class SecurityConfig
        {
            public bool EnableAutoLogin { get; set; } = false;
            public int SessionTimeout { get; set; } = 30;
            public bool RequireStrongPassword { get; set; } = false;
        }
    }

    public class ConfigModuleInitializer : IModuleInitializer
    {
        public string ModuleName => "配置服务";
        public string ModuleType => "Config";
        public InitializerPriority Priority => InitializerPriority.Core;
        public int Order => 1;
        public List<ModuleDependency> Dependencies => new();

        private readonly string configDir = "Config";
        private readonly string mainConfigFile = "Config/appsettings.json";
        private readonly string envConfigFile = "Config/appsettings.Production.json";

        private FileSystemWatcher watcher;

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

        // =============================
        // 1. 确保 Config 目录存在
        // =============================
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

        // =============================
        // 2. 加载主配置 + 环境配置（自动合并）
        // =============================
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

        private void MergeConfig(AppConfig baseConfig, AppConfig overrideConfig)
        {
            // 只覆盖非空字段，确保灵活
            baseConfig.UI.Theme = overrideConfig.UI.Theme ?? baseConfig.UI.Theme;
            baseConfig.UI.Language = overrideConfig.UI.Language ?? baseConfig.UI.Language;

            baseConfig.Database.ConnectionString =
                overrideConfig.Database.ConnectionString ?? baseConfig.Database.ConnectionString;
        }

        // =============================
        // 3. 验证配置
        // =============================
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

        // =============================
        // 4. 注册为全局可访问配置
        // =============================
        private void RegisterToGlobal(AppConfig config, Logger logger)
        {
            AppConfiguration.Current = config;

            Application.Current.Resources["AppConfig"] = config;

            logger.Config($"主题：{config.UI.Theme}");
            logger.Config($"语言：{config.UI.Language}");
        }

        // =============================
        // 5. 如果主配置不存在则保存
        // =============================
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

        // =============================
        // 6. 自动备份损坏的 JSON
        // =============================
        private async Task BackupCorruptedConfigAsync(string file, Logger logger)
        {
            string backup =
                $"Config/Backups/{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            File.Copy(file, backup, true);
            logger.Warning($"已备份损坏的配置文件 → {backup}");
            await Task.CompletedTask;
        }

        // =============================
        // 7. 配置热更新（实时监听）
        // =============================
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

        // =============================
        // 8. 创建默认配置（补齐缺失方法）
        // =============================
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
    }

    // =============================
    // 9. 全局配置访问（放在同一文件以便替换）
    // =============================
    public static class AppConfiguration
    {
        public static AppConfig Current { get; set; } = new AppConfig();

        // 保持事件封装，外部只能订阅/退订
        public static event Action? OnConfigChanged;

        /// <summary>
        /// 在类内部安全触发 OnConfigChanged 事件（会尽量在 UI 线程执行）
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
    }
}
