using AVDump3Lib;
using AVDump3Lib.Information;
using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3CL {
    public class AVD3CLModule : IAVD3Module {
        private AVD3CLModuleSettings settings = new AVD3CLModuleSettings();

        private IAVD3ProcessingModule processingModule;
        private IAVD3InformationModule informationModule;
        private IAVD3ReportingModule reportingModule;
        private AVD3CL cl;

        public AVD3CLModule() {
            AppDomain.CurrentDomain.UnhandledException += UnhandleException;
        }

        private void UnhandleException(object sender, UnhandledExceptionEventArgs e) {
            var wrapEx = new AVD3CLException("Unhandled AppDomain wide Exception", 
                e.ExceptionObject as Exception ?? new Exception("Non Exception Type: " + e.ExceptionObject.ToString()));
            OnException(wrapEx);
        }

        private void OnException(AVD3CLException ex) {
            var exElem = ex.ToXElement(
                settings.Diagnostics.SkipEnvironmentElement,
                settings.Diagnostics.IncludePersonalData
            );
            //TODO Raise Event for modules to listen to

            if(settings.Diagnostics.SaveErrors) {
                Directory.CreateDirectory(Path.GetDirectoryName(settings.Diagnostics.ErrorDirectory));
                var filePath = Path.Combine(settings.Diagnostics.ErrorDirectory, "AVD3Error" + ex.ThrownOn.ToString("yyyyMMdd HHmmssffff") + ".xml");

                using(var safeXmlWriter = new SafeXmlWriter(filePath, Encoding.UTF8)) {
                    exElem.WriteTo(safeXmlWriter);
                }
            }

        }

        public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
            processingModule = modules.OfType<IAVD3ProcessingModule>().Single();
            informationModule = modules.OfType<IAVD3InformationModule>().Single();
            reportingModule = modules.OfType<IAVD3ReportingModule>().Single();

            var settingsgModule = modules.OfType<IAVD3SettingsModule>().Single();
            settingsgModule.RegisterCommandlineArgs += CreateCommandlineArguments;

        }

        public void Process(string[] paths) {
            //if(UsedBlockConsumerNames.Count == 0) {
            //	Console.WriteLine("No Blockconsumer chosen: Nothing to do");
            //	return;
            //}

            var bcs = new BlockConsumerSelector(processingModule.BlockConsumerFactories);
            bcs.Filter += BlockConsumerFilter;

            var bp = new BlockPool(settings.Processing.BlockCount, settings.Processing.BlockLength);

            var fileDiscoveryOn = DateTimeOffset.UtcNow;
            var acceptedFiles = 0;

            var scf = new StreamConsumerFactory(bcs, bp);
            var sp = new StreamFromPathsProvider(settings.FileDiscovery.GlobalConcurrentCount,
                settings.FileDiscovery.PathPartitions, paths, true,
                path => {
                    if(fileDiscoveryOn.AddSeconds(1) < DateTimeOffset.UtcNow) {
                        Console.WriteLine("Accepted files: " + acceptedFiles);
                        Console.CursorTop--;
                        fileDiscoveryOn = DateTimeOffset.UtcNow;
                    }

                    var accept = false;
                    if(settings.FileDiscovery.FileExtensions.Items.Count == 0) accept = true;


                    accept = !settings.FileDiscovery.FileExtensions.Allow ^
                        settings.FileDiscovery.FileExtensions.Items.Any(
                            fe => path.EndsWith(fe, StringComparison.InvariantCultureIgnoreCase));

                    if(accept) acceptedFiles++;

                    return accept;
                },
                ex => Console.Error.WriteLine("Filediscovery: " + ex.Message)
            );

            //sp = new NullStreamProvider();

            var streamConsumerCollection = new StreamConsumerCollection(scf, sp);
            var bytesReadProgress = new BytesReadProgress(processingModule.BlockConsumerFactories.Select(x => x.Name));

            cl = new AVD3CL(settings.Display, bytesReadProgress.GetProgress);
            cl.TotalFiles = sp.TotalFileCount;
            cl.TotalBytes = sp.TotalBytes;

            cl.Display();

            streamConsumerCollection.ConsumingStream += ConsumingStream;

            Console.CursorVisible = false;
            try {
                streamConsumerCollection.ConsumeStreams(CancellationToken.None, bytesReadProgress);

            } catch(Exception ex) {
                Console.Error.WriteLine(ex);

            } finally {
                cl.Stop();
                Console.CursorVisible = true;
            }

            sp.Dispose();
            cl.Dispose();
        }

        private void BlockConsumerFilter(object sender, BlockConsumerSelectorEventArgs e) {
            e.Select = settings.Processing.UsedBlockConsumerNames.Any(x => e.Name.Equals(x, StringComparison.OrdinalIgnoreCase));
        }

        private async void ConsumingStream(object sender, ConsumingStreamEventArgs e) {
            e.OnException += (s, args) => {
                args.IsHandled = true;
                args.Retry = args.RetryCount < 2;

                OnException(new AVD3CLException("ConsumingStream", args.Cause));
            };

            var blockConsumers = await e.FinishedProcessing;

            var filePath = (string)e.Tag;
            var fileName = Path.GetFileName(filePath);

            if(settings.Display.PrintHashes || settings.Display.PrintReports) {
                cl.Writeline(fileName.Substring(0, Math.Min(fileName.Length, Console.WindowWidth - 1)));
            }

            if(settings.Display.PrintHashes) {
                foreach(var bc in blockConsumers.OfType<HashCalculator>()) {
                    cl.Writeline(bc.Name + " => " + BitConverter.ToString(bc.HashAlgorithm.Hash).Replace("-", ""));
                }
                cl.Writeline("");
            }

            var infoSetup = new InfoProviderSetup(filePath, blockConsumers);
            var infoProviders = informationModule.InfoProviderFactories.Select(x => x.Create(infoSetup));

            var reportsFactories = reportingModule.ReportFactories.Where(x => settings.Reporting.UsedReportNames.Any(y => x.Name.Equals(y, StringComparison.OrdinalIgnoreCase))).ToArray();

            if(reportsFactories.Length != 0) {
                var fileMetaInfo = new FileMetaInfo(new FileInfo(filePath), infoProviders);
                var reports = reportsFactories.Select(x => x.Create(fileMetaInfo));

                foreach(var report in reports) {
                    if(settings.Display.PrintReports) {
                        cl.Writeline(report.ReportToString() + "\n");
                    }

                    report.SaveToFile(Path.Combine(settings.Reporting.ReportDirectory, fileName + "." + report.FileExtension));
                }
            }


            //if(UseNtfsAlternateStreams) {
            //	using(var altStreamHandle = NtfsAlternateStreams.SafeCreateFile(
            //		NtfsAlternateStreams.BuildStreamPath((string)e.Tag, "AVDump3.xml"),
            //		NtfsAlternateStreams.ToNative(FileAccess.ReadWrite), FileShare.None,
            //		IntPtr.Zero, FileMode.OpenOrCreate, 0, IntPtr.Zero))
            //	using(var altStream = new FileStream(altStreamHandle, FileAccess.ReadWrite)) {
            //		var avd3Elem = new XElement("AVDump3",
            //		  new XElement("Revision",
            //			new XAttribute("Build", Assembly.GetExecutingAssembly().GetName().Version.Build),
            //			blockConsumers.OfType<HashCalculator>().Select(hc =>
            //			  new XElement(hc.HashAlgorithmType.Key, BitConverter.ToString(hc.HashAlgorithm.Hash).Replace("-", ""))
            //			)
            //		  )
            //		);
            //		avd3Elem.Save(altStream, SaveOptions.None);
            //	}
            //}


        }

        private IEnumerable<ArgGroup> CreateCommandlineArguments() {
            var availableBlockConsumerNames = processingModule.BlockConsumerFactories.Select(x => x.Name).ToArray();
            var availableReportNames = reportingModule.ReportFactories.Select(x => x.Name).ToArray();

            #region Processing
            yield return new ArgGroup("Processing",
                "",
                ArgStructure.Create(
                    arg => {
                        var raw = arg.Split(':').Select(ldArg => int.Parse(ldArg));
                        return new { BlockSize = raw.ElementAt(0), BlockCount = raw.ElementAt(1) };
                    },
                    args => {
                        settings.Processing.BlockCount = args.BlockCount;
                        settings.Processing.BlockLength = args.BlockSize << 20;
                    },
                    "--BSize=<blocksize in kb>:<block count>",
                    "Circular buffer size for hashing",
                    "BlockSize", "BSize"
                ),
                ArgStructure.Create(
                    arg => arg.Split(',').Select(a => a.Trim()),
                    hashNames => settings.Processing.UsedBlockConsumerNames = Array.AsReadOnly(hashNames.ToArray()),
                    "--Consumers=<ConsumerName1>[,<ConsumerName2>,...]",
                    "Select consumers to use (" + string.Join(", ", availableBlockConsumerNames) + ")",
                    "Consumers", "Cons"
                )
            );
            #endregion

            #region Reporting
            yield return new ArgGroup("Reporting",
                "",
                ArgStructure.Create(
                    arg => arg.Split(',').Select(a => a.Trim()),
                    reportNames => settings.Reporting.UsedReportNames = Array.AsReadOnly(reportNames.ToArray()),
                    "--Reports=<ReportName1>[,<ReportName2>,...]",
                    string.Join(", ", availableReportNames),
                    "Reports"
                ),
                ArgStructure.Create(
                    arg => settings.Reporting.ReportDirectory = arg,
                    "--ReportDirectory=<DirectoryPath>",
                    "",
                    "ReportDirectory", "RDir"
                )
            );
            #endregion

            #region FileDiscovery
            yield return new ArgGroup("FileDiscovery",
                "",
                ArgStructure.Create(
                    arg => settings.FileDiscovery.Recursive = true,
                    "--Recursive",
                    "",
                    "Recursive", "R"
                ),
                ArgStructure.Create(
                    arg => {
                        settings.FileDiscovery.FileExtensions.Allow = arg[0] != '-';
                        if(arg[0] == '-') arg = arg.Substring(1);
                        settings.FileDiscovery.FileExtensions.Items = Array.AsReadOnly(arg.Split(','));
                    },
                    "--WithExtensions=[-]<Extension1>[,<Extension2>,...]",
                    "",
                    "WithExtensions", "WExts"
                ),
                ArgStructure.Create(
                    arg => {
                        var raw = arg.Split(new char[] { ':' }, 2);
                        return new {
                            MaxCount = int.Parse(raw[0]),
                            PerPath =
                              from item in (raw.Length > 1 ? raw[1].Split(';') : new string[0])
                              let parts = item.Split(',')
                              select new { Path = parts[0], MaxCount = int.Parse(parts[1]) }
                        };
                    },
                    arg => {
                        settings.FileDiscovery.GlobalConcurrentCount = arg.MaxCount;
                        settings.FileDiscovery.PathPartitions = Array.AsReadOnly(arg.PerPath.Select(x => new PathPartition(x.Path, x.MaxCount)).ToArray());
                    },
                    "--Concurrent=<max>[:<path1>,<max1>;<path2>,<max2>;...]",
                    "Sets the maximal number of files which will be processed concurrently.\n" +
                    "First param (max) sets a global limit. (path,max) pairs sets limits per path.",
                    "Concurrent", "Conc"
                )
            );
            #endregion

            #region Display
            yield return new ArgGroup("Display",
                "",
                ArgStructure.Create(
                    arg => settings.Display.HideBuffers = true,
                    "--HideBuffers",
                    "",
                    "HideBuffers"
                ),
                ArgStructure.Create(
                    arg => settings.Display.HideFileProgress = true,
                    "--HideFileProgress",
                    "",
                    "HideFileProgress"
                ),
                ArgStructure.Create(
                    arg => settings.Display.HideTotalProgress = true,
                    "--HideTotalProgress",
                    "",
                    "HideTotalProgress"
                ),
                ArgStructure.Create(
                    arg => {
                        settings.Display.HideBuffers = true;
                        settings.Display.HideFileProgress = true;
                        settings.Display.HideTotalProgress = true;
                    },
                    "--HideUI",
                    "",
                    "HideUI"
                ),
                ArgStructure.Create(
                    arg => settings.Display.PrintHashes = true,
                    "--PrintHashes",
                    "",
                    "PrintHashes"
                ),
                ArgStructure.Create(
                    arg => settings.Display.PrintReports = true,
                    "--PrintReports",
                    "",
                    "PrintReports"
                )
            );
            #endregion

            #region Diagnostics
            yield return new ArgGroup("Diagnostics",
                "",
                ArgStructure.Create(
                    _ => settings.Diagnostics.SaveErrors = true,
                    "--SaveErrors",
                    "",
                    "SaveErrors"
                ),
                ArgStructure.Create(
                    _ => settings.Diagnostics.SkipEnvironmentElement = true,
                    "--SkipEnvironmentElement",
                    "",
                    "SkipEnvironmentElement"
                ),
                ArgStructure.Create(
                    _ => settings.Diagnostics.IncludePersonalData = true,
                    "--IncludePersonalData",
                    "",
                    "IncludePersonalData"
                ),
                ArgStructure.Create(
                    arg => settings.Diagnostics.ErrorDirectory = arg,
                    "--ErrorDirectory=<DirectoryPath>",
                    "",
                    "ErrorDirectory", "ErrDir"
                )
            );
            #endregion

            //bool useNtfsAlternateStreams = false;
            //yield return new ArgGroup("Internal",
            //	"",
            //	() => {
            //		UseNtfsAlternateStreams = useNtfsAlternateStreams;
            //	},
            //	ArgStructure.Create(
            //		arg => useNtfsAlternateStreams = true,
            //		"--UseNtfsAlternateStreams",
            //		"Store Hashes in Ntfs Alternate Streams to avoid unecessary rehashing",
            //		"UseNtfsAlternateStreams"
            //	)
            //);
        }
    }

    public class AVD3CLException : AVD3LibException {
        public AVD3CLException(string message, Exception innerException) : base(message, innerException)  {

        }
    }
}
