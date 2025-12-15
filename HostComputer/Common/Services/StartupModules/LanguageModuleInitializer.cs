using MyLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace HostComputer.Common.Services.StartupModules
{
    /// <summary>
    /// 启动模块：多语言加载（ClosedXML 实现）
    /// - 默认从 {AppBase}/Language/language.xlsx 加载
    /// - 支持热更新（FileSystemWatcher 防抖）
    /// - 初始化失败时回退内置默认语言
    /// </summary>
    public class LanguageModuleInitializer : IModuleInitializer, IDisposable
    {
        public string ModuleName => "多语言服务";
        public string ModuleType => "Language";
        public InitializerPriority Priority => InitializerPriority.Core;
        public int Order => 3;
        public List<ModuleDependency> Dependencies => new();

        private readonly string[] _candidatePaths;
        private readonly bool _enableHotReload;
        private FileSystemWatcher? _watcher;
        private readonly object _sync = new();
        private Timer? _debounceTimer; // 防抖定时器
        private const int DebounceMs = 500; // 防抖时间
        private bool _disposed = false;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="enableHotReload">是否启用热更新（监听文件变更）</param>
        /// <param name="customPaths">可选的语言文件候选路径，优先级从前到后</param>
        public LanguageModuleInitializer(bool enableHotReload = true, params string[] customPaths)
        {
            _enableHotReload = enableHotReload;

            // 默认路径优先使用应用目录下的 Language/language.xlsx
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var defaultPath = Path.Combine(baseDir, "Language", "language.xlsx");

            // 允许传入自定义路径，最后追加默认路径
            var list = new List<string>();
            if (customPaths != null && customPaths.Length > 0)
            {
                foreach (var p in customPaths)
                {
                    if (!string.IsNullOrWhiteSpace(p)) list.Add(p);
                }
            }
            list.Add(defaultPath);
            _candidatePaths = list.ToArray();
        }

        /// <summary>
        /// 初始化入口（由启动器调用）
        /// </summary>
        public async Task<bool> InitializeAsync(Logger logger)
        {
            logger.Config("开始加载多语言资源...");

            try
            {
                // 找到可用的语言文件
                var file = FindLanguageFile(logger);
                if (file == null)
                {
                    logger.Warning("语言文件未找到，使用默认内置语言");
                    return await InitializeDefaultLanguageAsync(logger);
                }

                // 检查是否可访问
                if (!await IsFileReadableAsync(file, logger))
                {
                    logger.Warning("语言文件不可读，使用默认内置语言");
                    return await InitializeDefaultLanguageAsync(logger);
                }

                // 读取数据（IO 密集，放到线程池线程）
                var languageData = await Task.Run(() => LoadLanguageData(file, logger));

                // 初始化 App.Lang
                ApplyToLanguageService(languageData, logger);

                logger.Config("多语言服务初始化成功");

                // 启用热更新（可选）
                if (_enableHotReload)
                {
                    try
                    {
                        StartWatcher(Path.GetDirectoryName(file) ?? AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(file), logger);
                        logger.Config("语言文件热更新已启用");
                    }
                    catch (Exception ex)
                    {
                        logger.Warning($"无法启用热更新: {ex.Message}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"多语言模块加载失败: {ex.Message}");
                logger.Debug(ex.ToString());
                return await InitializeDefaultLanguageAsync(logger);
            }
        }

        #region 文件查找与访问

        private string? FindLanguageFile(Logger logger)
        {
            foreach (var p in _candidatePaths)
            {
                try
                {
                    var full = Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p));
                    if (File.Exists(full))
                    {
                        logger.Config($"找到语言文件: {full}");
                        return full;
                    }
                }
                catch (Exception ex)
                {
                    logger.Debug($"检查路径失败 {p}: {ex.Message}");
                }
            }
            return null;
        }

        private async Task<bool> IsFileReadableAsync(string path, Logger logger)
        {
            // 尝试以共享读方式打开
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await Task.Delay(1); // 确保异步上下文
                    logger.Config($"语言文件可访问: {path}");
                }
                return true;
            }
            catch (IOException ioEx)
            {
                logger.Error($"语言文件被占用或不可访问: {ioEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                logger.Error($"检查语言文件失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ClosedXML 读取

        /// <summary>
        /// 使用 ClosedXML 读取 language.xlsx
        /// 文件格式：
        /// 第一行：第一列为 Key（或 Key），第二列及以后为语言代码（例如 zh-CN, en-US）
        /// 后续行：第一列为翻译 Key，后面列为对应语言的翻译
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> LoadLanguageData(string filePath, Logger logger)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                logger.Warning("语言文件工作表为空");
                return result;
            }

            // 发现最后使用的行列
            var firstRow = ws.Row(1);
            var lastColumn = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 1;
            var lastRow = ws.Column(1).LastCellUsed()?.Address.RowNumber ?? 1;

            logger.Config($"语言文件 行={lastRow} 列={lastColumn}");

            // 读取语言代码（从第2列开始）
            var langList = new List<string>();
            for (int c = 2; c <= lastColumn; c++)
            {
                var code = ws.Cell(1, c).GetString().Trim();
                if (string.IsNullOrEmpty(code)) continue;
                if (!langList.Contains(code, StringComparer.OrdinalIgnoreCase))
                {
                    langList.Add(code);
                    result[code] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    logger.Config($"检测到语言: {code}");
                }
            }

            if (langList.Count == 0)
            {
                logger.Warning("未在第一行检测到任何语言代码");
            }

            // 读取翻译行（从第2行开始）
            for (int r = 2; r <= lastRow; r++)
            {
                var key = ws.Cell(r, 1).GetString().Trim();
                if (string.IsNullOrEmpty(key)) continue;

                for (int i = 0; i < langList.Count; i++)
                {
                    var lang = langList[i];
                    var text = ws.Cell(r, i + 2).GetString(); // 可能为空字符串
                    result[lang][key] = text ?? string.Empty;
                }
            }

            // 日志统计
            foreach (var lang in result.Keys)
            {
                logger.Config($"语言 {lang} 加载了 {result[lang].Count} 条翻译");
            }

            return result;
        }

        #endregion

        #region 应用到 LanguageService

        private void ApplyToLanguageService(Dictionary<string, Dictionary<string, string>> languageData, Logger logger)
        {
            // 安全地把 languageData 转换为 LanguageService 所需的结构（lang -> (key->text)）已是该结构
            lock (_sync)
            {
                // 把数据赋值给 App.Lang
                App.Lang.Initialize(languageData);

                // 设置默认语言：优先 en-US，否则第一个可用语言
                //var defaultLang = App.Lang.AvailableLanguages.Contains("en-US", StringComparer.OrdinalIgnoreCase)
                //    ? "en-US"
                //    : App.Lang.AvailableLanguages.FirstOrDefault() ?? "en-US";

                // 切换 CurrentLang（在 UI 线程安全地做）
                try
                {
                    // 如果当前在非 UI 线程，Dispatcher 会把它转入 UI 线程
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() => App.Lang.CurrentLang = AppConfiguration.Current.UI.Language);
                }
                catch
                {
                    // 如果无法访问 Dispatcher，就直接赋值（非 UI 环境）
                    App.Lang.CurrentLang = AppConfiguration.Current.UI.Language;
                }

                logger.Config($"默认语言已设置为: {AppConfiguration.Current.UI.Language}");
            }
        }

        #endregion

        #region 默认语言回退

        private Task<bool> InitializeDefaultLanguageAsync(Logger logger)
        {
            var defaultData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["zh-CN"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Login"] = "登录",
                    ["Username"] = "用户名",
                    ["Password"] = "密码",
                    ["Welcome"] = "欢迎使用 Host Computer System",
                },
                ["en-US"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Login"] = "Login",
                    ["Username"] = "Username",
                    ["Password"] = "Password",
                    ["Welcome"] = "Welcome to Host Computer System",
                }
            };

            ApplyToLanguageService(defaultData, logger);
            logger.Config("使用内置默认语言");
            return Task.FromResult(true);
        }

        #endregion

        #region 热更新（FileSystemWatcher）

        private void StartWatcher(string directory, string fileName, Logger logger)
        {
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                return;

            // 已经启用则不重复启用
            if (_watcher != null) return;

            _watcher = new FileSystemWatcher(directory)
            {
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            _watcher.Changed += (s, e) => OnFileChangedDebounced(e.FullPath, logger);
            _watcher.Renamed += (s, e) => OnFileChangedDebounced(e.FullPath, logger);
            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileChangedDebounced(string path, Logger logger)
        {
            // 防抖：每次触发都重置定时器，DebounceMs 后执行实际 reload
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                _debounceTimer?.Dispose();
                _debounceTimer = null;

                try
                {
                    logger.Config($"检测到语言文件变更，重新加载: {path}");

                    // 先检查文件是否可读（避免正在写入导致读取异常）
                    if (!File.Exists(path))
                    {
                        logger.Warning("变更文件不存在");
                        return;
                    }

                    if (!IsFileReadable(path, logger))
                    {
                        logger.Warning("变更文件目前不可读，跳过此次更新");
                        return;
                    }

                    // 读取并应用
                    var newData = LoadLanguageData(path, logger);
                    ApplyToLanguageService(newData, logger);
                    logger.Config("语言文件热更新应用完成");
                }
                catch (Exception ex)
                {
                    logger.Error($"热更新加载失败: {ex.Message}");
                    logger.Debug(ex.ToString());
                }

            }, null, DebounceMs, Timeout.Infinite);
        }

        // 同步检查可读（用于热更新路径, 避免 await）
        private bool IsFileReadable(string path, Logger logger)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return true;
            }
            catch (IOException ioEx)
            {
                logger.Debug($"文件被占用: {ioEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                logger.Debug($"检查文件可读性失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }

        #endregion
    }
}
