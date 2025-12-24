using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HostComputer.ViewModels.Overview
{
    public class SoftwareVersionViewModel
    {
        public string SoftwareVersion { get; }
        public string GitRevision { get; }
        public string CommitMessage { get; }
        public string BuildTime { get; }

        public SoftwareVersionViewModel()
        {
            var assembly = Assembly.GetExecutingAssembly();
            SoftwareVersion = assembly.GetName().Version?.ToString() ?? "N/A";

            GitRevision = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                  .FirstOrDefault(a => a.Key == "GitRevision")?.Value ?? "N/A";

            CommitMessage = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                                  .FirstOrDefault(a => a.Key == "GitCommitMessage")?.Value ?? "N/A";

            BuildTime = File.GetLastWriteTime(assembly.Location)
                            .ToString("yyyy-MM-dd HH:mm:ss");
        }

    }
}

