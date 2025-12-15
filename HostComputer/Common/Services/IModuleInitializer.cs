using MyLogger;

namespace HostComputer.Common.Services
{
    public enum InitializerPriority
    {
        Core,      // 核心服务 (最先执行)
        Business,  // 业务模块
        UI         // UI相关
    }

    public enum ModuleStatus
    {
        Pending,
        Success,
        Failed,
        Error,
        DependencyFailed,
        Skipped
    }

    public class ModuleInfo
    {
        public string Name { get; set; }
        public ModuleStatus Status { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public string Error { get; set; }
    }

    public class StartupResult
    {
        public TimeSpan TotalDuration { get; set; }
        public int ModuleCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<ModuleInfo> Modules { get; set; }

        public bool IsSuccess => FailedCount == 0;
        public double SuccessRate => ModuleCount > 0 ? (double)SuccessCount / ModuleCount : 0;
    }

    public class ModuleDependency
    {
        public string ModuleName { get; set; }
        public string ModuleType { get; set; }
    }

    public interface IModuleInitializer
    {
        string ModuleName { get; }
        string ModuleType { get; }
        InitializerPriority Priority { get; }
        int Order { get; }
        List<ModuleDependency> Dependencies { get; }
        Task<bool> InitializeAsync(Logger logger);
    }
}