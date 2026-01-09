using System;
using System.IO;

namespace HostComputer.Common
{
    /// <summary>
    /// 全局路径管理器（工业项目推荐）
    /// </summary>
    public static class PathManager
    {
        /// <summary>
        /// 解决方案根目录
        /// </summary>
        public static string SolutionRoot { get; }

        /// <summary>
        /// ConfigFile（唯一一级目录）
        /// </summary>
        public static string ConfigDir { get; }

        /// <summary>
        /// 软件配置
        /// </summary>
        public static string ConfigSoftware => EnsureSubDir("Config");

        /// <summary>
        /// 设备配置
        /// </summary>
        public static string ConfigDevice => EnsureSubDir("DeviceConfig");

        /// <summary>
        /// Recipe
        /// </summary>
        public static string RecipeDir => EnsureSubDir("Recipe");

        /// <summary>
        /// 日志
        /// </summary>
        public static string LogDir => EnsureSubDir("Logs");

        /// <summary>
        /// 数据
        /// </summary>
        public static string DataDir => EnsureSubDir("Data");

        /// <summary>
        /// 语言文件
        /// </summary>
        public static string LanguageDir => EnsureSubDir("Language");

        // ================= 初始化 =================

        static PathManager()
        {
            // bin/Debug/netXxx → 项目 → Solution
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = new DirectoryInfo(baseDir);

            while (dir != null && dir.GetFiles("*.sln").Length == 0)
            {
                dir = dir.Parent;
            }

            if (dir == null)
                throw new DirectoryNotFoundException("未找到解决方案根目录（.sln）");

            SolutionRoot = dir.FullName;

            // 确保 ConfigFile 是唯一一级目录
            ConfigDir = EnsureRootDir("ConfigFile");
        }

        // ================= 文件路径 =================

        public static string ConfigFile(string fileName)
            => Path.Combine(ConfigDir, fileName);

        public static string AppConfigFile(string fileName)
            => Path.Combine(ConfigSoftware, fileName);

        public static string DeviceConfigFile(string fileName)
            => Path.Combine(ConfigDevice, fileName);

        public static string DataFile(string fileName)
            => Path.Combine(DataDir, fileName);

        public static string LanguageFile(string fileName)
            => Path.Combine(LanguageDir, fileName);

        // ================= 私有方法 =================

        /// <summary>
        /// SolutionRoot 下的一级目录
        /// </summary>
        private static string EnsureRootDir(string folderName)
        {
            var path = Path.Combine(SolutionRoot, folderName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// ConfigFile 下的二级目录
        /// </summary>
        private static string EnsureSubDir(string folderName)
        {
            var path = Path.Combine(ConfigDir, folderName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }
}
