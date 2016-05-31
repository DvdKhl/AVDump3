using AVDump3Lib.Processing.StreamProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3CL {
    public class AVD3CLModuleSettings {
        public FileDiscoverySettings FileDiscovery { get; }
        public ProcessingSettings Processing { get; }
        public ReportingSettings Reporting { get; }
        public DisplaySettings Display { get; }
        public DiagnosticsSettings Diagnostics { get; }

        public bool UseNtfsAlternateStreams { get; set; }

        public AVD3CLModuleSettings() {
            FileDiscovery = new FileDiscoverySettings();
            Processing = new ProcessingSettings();
            Reporting = new ReportingSettings();
            Display = new DisplaySettings();
            Diagnostics = new DiagnosticsSettings();
        }
    }

    public class FileExtensionsSetting {
        public bool Allow { get; set; }
        public IReadOnlyList<string> Items { get; set; }

        public FileExtensionsSetting() {
            Allow = false;
            Items = Array.AsReadOnly(new string[0]);
        }
    }
    public class FileDiscoverySettings {
        public bool Recursive { get; set; }
        public int GlobalConcurrentCount { get; set; }
        public IReadOnlyList<PathPartition> PathPartitions { get; set; }
        public FileExtensionsSetting FileExtensions { get; }

        public FileDiscoverySettings() {
            Recursive = false;
            GlobalConcurrentCount = 1;
            PathPartitions = Array.AsReadOnly(new PathPartition[0]);
            FileExtensions = new FileExtensionsSetting();
        }
    }

    public class ProcessingSettings {
        public int BlockCount { get; set; }
        public int BlockLength { get; set; }
        public IReadOnlyList<string> UsedBlockConsumerNames { get; set; }

        public ProcessingSettings() {
            BlockCount = 8;
            BlockLength = 8 << 20;
            UsedBlockConsumerNames = Array.AsReadOnly(new string[0]);
        }
    }


    public class ReportingSettings {
        public IReadOnlyList<string> UsedReportNames { get; set; }
        public string ReportDirectory { get; set; }

        public ReportingSettings() {
            UsedReportNames = Array.AsReadOnly(new string[0]);
            ReportDirectory = Environment.CurrentDirectory;
        }
    }

    public class DisplaySettings {
        public bool HideBuffers { get; set; }
        public bool HideFileProgress { get; set; }
        public bool HideTotalProgress { get; set; }

        public bool PrintHashes { get; set; }
        public bool PrintReports { get; set; }
    }

    public class DiagnosticsSettings {
        public bool SaveErrors { get; set; }
        public bool SkipEnvironmentElement { get; set; }
        public bool IncludePersonalData { get; set; }
        public string ErrorDirectory { get; set; }

        public DiagnosticsSettings() {
            ErrorDirectory = Environment.CurrentDirectory;
        }
    }

}
