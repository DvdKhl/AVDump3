﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AVDump3UI {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Lang {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Lang() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("AVDump3UI.Lang", typeof(Lang).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --ErrorDirectory=&lt;DirectoryPath&gt;.
        /// </summary>
        internal static string Diagnostics_ErrorDirectory_Description {
            get {
                return ResourceManager.GetString("Diagnostics.ErrorDirectory.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to If --SaveErrors is specified the error files will be placed in the specified path.
        /// </summary>
        internal static string Diagnostics_ErrorDirectory_Example {
            get {
                return ResourceManager.GetString("Diagnostics.ErrorDirectory.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Various places may include personal data. Currently this only affects error files, which will then include the full filepath.
        /// </summary>
        internal static string Diagnostics_IncludePersonalData_Description {
            get {
                return ResourceManager.GetString("Diagnostics.IncludePersonalData.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --IncludePersonalData.
        /// </summary>
        internal static string Diagnostics_IncludePersonalData_Example {
            get {
                return ResourceManager.GetString("Diagnostics.IncludePersonalData.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use Memory as the DataSource for HashSpeed testing. Overrides any FileDiscovery Settings!.
        /// </summary>
        internal static string Diagnostics_NullStreamTest_Description {
            get {
                return ResourceManager.GetString("Diagnostics.NullStreamTest.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --NullStreamTest=&lt;StreamCount&gt;:&lt;StreamLength in MiB&gt;:&lt;ParallelStreamCount&gt;.
        /// </summary>
        internal static string Diagnostics_NullStreamTest_Example {
            get {
                return ResourceManager.GetString("Diagnostics.NullStreamTest.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Errors occuring during program execution will be saved to disk.
        /// </summary>
        internal static string Diagnostics_SaveErrors_Description {
            get {
                return ResourceManager.GetString("Diagnostics.SaveErrors.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --SaveErrors.
        /// </summary>
        internal static string Diagnostics_SaveErrors_Example {
            get {
                return ResourceManager.GetString("Diagnostics.SaveErrors.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Skip the environment element in error files.
        /// </summary>
        internal static string Diagnostics_SkipEnvironmentElement_Description {
            get {
                return ResourceManager.GetString("Diagnostics.SkipEnvironmentElement.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --SkipEnvironmentElement.
        /// </summary>
        internal static string Diagnostics_SkipEnvironmentElement_Example {
            get {
                return ResourceManager.GetString("Diagnostics.SkipEnvironmentElement.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The cursor position of the console will not be explicitly set. This option will disable most progress output.
        /// </summary>
        internal static string Display_ForwardConsoleCursorOnly_Description {
            get {
                return ResourceManager.GetString("Display.ForwardConsoleCursorOnly.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --ForwardConsoleCursorOnly.
        /// </summary>
        internal static string Display_ForwardConsoleCursorOnly_Example {
            get {
                return ResourceManager.GetString("Display.ForwardConsoleCursorOnly.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Hides buffer bars.
        /// </summary>
        internal static string Display_HideBuffers_Description {
            get {
                return ResourceManager.GetString("Display.HideBuffers.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --HideBuffers.
        /// </summary>
        internal static string Display_HideBuffers_Example {
            get {
                return ResourceManager.GetString("Display.HideBuffers.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Hides file progress.
        /// </summary>
        internal static string Display_HideFileProgress_Description {
            get {
                return ResourceManager.GetString("Display.HideFileProgress.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --HideFileProgress.
        /// </summary>
        internal static string Display_HideFileProgress_Example {
            get {
                return ResourceManager.GetString("Display.HideFileProgress.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Hides total progress.
        /// </summary>
        internal static string Display_HideTotalProgress_Description {
            get {
                return ResourceManager.GetString("Display.HideTotalProgress.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --HideTotalProgress.
        /// </summary>
        internal static string Display_HideTotalProgress_Example {
            get {
                return ResourceManager.GetString("Display.HideTotalProgress.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Displays the time taken to calculate progression stats and drawing to console.
        /// </summary>
        internal static string Display_ShowDisplayJitter_Description {
            get {
                return ResourceManager.GetString("Display.ShowDisplayJitter.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --ShowDisplayJitter.
        /// </summary>
        internal static string Display_ShowDisplayJitter_Example {
            get {
                return ResourceManager.GetString("Display.ShowDisplayJitter.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sets the maximal number of files which will be processed concurrently.
        ///First param (max) sets a global limit. (path,max) pairs sets limits per path..
        /// </summary>
        internal static string FileDiscovery_Concurrent_Description {
            get {
                return ResourceManager.GetString("FileDiscovery.Concurrent.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --Concurrent=&lt;max&gt;[:&lt;path1&gt;,&lt;max1&gt;;&lt;path2&gt;,&lt;max2&gt;;...].
        /// </summary>
        internal static string FileDiscovery_Concurrent_Example {
            get {
                return ResourceManager.GetString("FileDiscovery.Concurrent.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Will set --SkipLogPath and --ProcessedLogPath to the specified filepath.
        /// </summary>
        internal static string FileDiscovery_DoneLogPath_Description {
            get {
                return ResourceManager.GetString("FileDiscovery.DoneLogPath.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --DoneLogPath=&lt;Filepath&gt;.
        /// </summary>
        internal static string FileDiscovery_DoneLogPath_Example {
            get {
                return ResourceManager.GetString("FileDiscovery.DoneLogPath.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Appends the full filepath to the specified path.
        /// </summary>
        internal static string FileDiscovery_ProcessedLogPath_Description {
            get {
                return ResourceManager.GetString("FileDiscovery.ProcessedLogPath.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --ProcessedLogPath=&lt;Filepath&gt;.
        /// </summary>
        internal static string FileDiscovery_ProcessedLogPath_Example {
            get {
                return ResourceManager.GetString("FileDiscovery.ProcessedLogPath.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Recursively descent into Subdirectories.
        /// </summary>
        internal static string FileDiscovery_Recursive_Description {
            get {
                return ResourceManager.GetString("FileDiscovery.Recursive.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --Recursive.
        /// </summary>
        internal static string FileDiscovery_Recursive_Example {
            get {
                return ResourceManager.GetString("FileDiscovery.Recursive.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Filepaths contained in the specified file will not be processed.
        /// </summary>
        internal static string FileDiscovery_SkipLogPath_Description {
            get {
                return ResourceManager.GetString("FileDiscovery.SkipLogPath.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --SkipLogPath=&lt;FilePath&gt;.
        /// </summary>
        internal static string FileDiscovery_SkipLogPath_Example {
            get {
                return ResourceManager.GetString("FileDiscovery.SkipLogPath.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only/Don&apos;t Process files with selected Extensions.
        /// </summary>
        internal static string FileDiscovery_WithExtensions_Description {
            get {
                return ResourceManager.GetString("FileDiscovery.WithExtensions.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --WithExtensions=[-]&lt;Extension1&gt;[,&lt;Extension2&gt;,...].
        /// </summary>
        internal static string FileDiscovery_WithExtensions_Example {
            get {
                return ResourceManager.GetString("FileDiscovery.WithExtensions.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string FileMove_DisableFileMove_Description {
            get {
                return ResourceManager.GetString("FileMove.DisableFileMove.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --FileMove.DisableFileMove.
        /// </summary>
        internal static string FileMove_DisableFileMove_Example {
            get {
                return ResourceManager.GetString("FileMove.DisableFileMove.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string FileMove_DisableFileRename_Description {
            get {
                return ResourceManager.GetString("FileMove.DisableFileRename.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --FileMove.DisableFileRename.
        /// </summary>
        internal static string FileMove_DisableFileRename_Example {
            get {
                return ResourceManager.GetString("FileMove.DisableFileRename.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string FileMove_LogPath_Description {
            get {
                return ResourceManager.GetString("FileMove.LogPath.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --FileMove.LogPath=&lt;FilePath&gt;.
        /// </summary>
        internal static string FileMove_LogPath_Example {
            get {
                return ResourceManager.GetString("FileMove.LogPath.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string FileMove_Mode_Description {
            get {
                return ResourceManager.GetString("FileMove.Mode.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --FileMove.Mode=&lt;None|PlaceholderInline|PlaceholderFile|CSharpScriptInline|CSharpScriptFile|DotNetAssembly&gt;.
        /// </summary>
        internal static string FileMove_Mode_Example {
            get {
                return ResourceManager.GetString("FileMove.Mode.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Available Placeholders ${Name}:
        ///FullName, FileName, FileExtension, FileNameWithoutExtension, DirectoryName, SuggestedExtension,
        ///Hash-&lt;Name&gt;-&lt;2|4|8|10|16|32|32Hex|32Z|36|62|64&gt;-&lt;OC|UC|LC&gt;.
        /// </summary>
        internal static string FileMove_Pattern_Description {
            get {
                return ResourceManager.GetString("FileMove.Pattern.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --FileMove.Pattern=${DirectoryName}\${FileNameWithoutExtension}[${Hash-CRC32-16-UC}]${SuggestedExtension}.
        /// </summary>
        internal static string FileMove_Pattern_Example {
            get {
                return ResourceManager.GetString("FileMove.Pattern.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string FileMove_Replacements_Description {
            get {
                return ResourceManager.GetString("FileMove.Replacements.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --FileMove.Replacements=&lt;Match&gt;,&lt;Replacement&gt;[;&lt;Match&gt;,&lt;Replacement&gt;...].
        /// </summary>
        internal static string FileMove_Replacements_Example {
            get {
                return ResourceManager.GetString("FileMove.Replacements.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Circular buffer size for hashing.
        /// </summary>
        internal static string Processing_BufferLength_Description {
            get {
                return ResourceManager.GetString("Processing.BufferLength.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --BufferLength=&lt;Size in MiB&gt;.
        /// </summary>
        internal static string Processing_BufferLength_Example {
            get {
                return ResourceManager.GetString("Processing.BufferLength.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select consumers to use. Use without arguments to list available consumers.
        /// </summary>
        internal static string Processing_Consumers_Description {
            get {
                return ResourceManager.GetString("Processing.Consumers.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --Consumers=&lt;ConsumerName1&gt;[,&lt;ConsumerName2&gt;,...].
        /// </summary>
        internal static string Processing_Consumers_Example {
            get {
                return ResourceManager.GetString("Processing.Consumers.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Pause console before exiting.
        /// </summary>
        internal static string Processing_PauseBeforeExit_Description {
            get {
                return ResourceManager.GetString("Processing.PauseBeforeExit.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --PauseBeforeExit.
        /// </summary>
        internal static string Processing_PauseBeforeExit_Example {
            get {
                return ResourceManager.GetString("Processing.PauseBeforeExit.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Searches the filename for the calculated CRC32 hash. If not present or different a line with the caluclated hash and the full path of the file is appended to the specified path
        ///The regex pattern should contain the placeholder ${CRC32} which is replaced by the calculated hash prior matching.
        ///Consumer CRC32 will be force enabled!.
        /// </summary>
        internal static string Reporting_CRC32Error_Description {
            get {
                return ResourceManager.GetString("Reporting.CRC32Error.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --CRC32Error=&lt;Filepath&gt;:&lt;RegexPattern&gt;.
        /// </summary>
        internal static string Reporting_CRC32Error_Example {
            get {
                return ResourceManager.GetString("Reporting.CRC32Error.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Test.
        /// </summary>
        internal static string Reporting_Description {
            get {
                return ResourceManager.GetString("Reporting.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Logs the filepath if the detected extension does not match the actual extension.
        /// </summary>
        internal static string Reporting_ExtensionDifferencePath_Description {
            get {
                return ResourceManager.GetString("Reporting.ExtensionDifferencePath.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --EDPath=extdiff.txt.
        /// </summary>
        internal static string Reporting_ExtensionDifferencePath_Example {
            get {
                return ResourceManager.GetString("Reporting.ExtensionDifferencePath.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Print calculated hashes in hexadecimal format to console.
        /// </summary>
        internal static string Reporting_PrintHashes_Description {
            get {
                return ResourceManager.GetString("Reporting.PrintHashes.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --PrintHashes.
        /// </summary>
        internal static string Reporting_PrintHashes_Example {
            get {
                return ResourceManager.GetString("Reporting.PrintHashes.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Print generated reports to console.
        /// </summary>
        internal static string Reporting_PrintReports_Description {
            get {
                return ResourceManager.GetString("Reporting.PrintReports.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --PrintReports.
        /// </summary>
        internal static string Reporting_PrintReports_Example {
            get {
                return ResourceManager.GetString("Reporting.PrintReports.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reports will be saved to the specified directory.
        /// </summary>
        internal static string Reporting_ReportDirectory_Description {
            get {
                return ResourceManager.GetString("Reporting.ReportDirectory.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --ReportDirectory=&lt;Directory&gt;.
        /// </summary>
        internal static string Reporting_ReportDirectory_Example {
            get {
                return ResourceManager.GetString("Reporting.ReportDirectory.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reports will be saved/appended to the specified filename
        ///The following placeholders ${Name} can be used: FileName, FileNameWithoutExtension, FileExtension, ReportName, ReportFileExtension.
        /// </summary>
        internal static string Reporting_ReportFileName_Description {
            get {
                return ResourceManager.GetString("Reporting.ReportFileName.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --ReportFileName=&lt;FileName&gt;.
        /// </summary>
        internal static string Reporting_ReportFileName_Example {
            get {
                return ResourceManager.GetString("Reporting.ReportFileName.Example", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Select reports to use. Use without arguments to list available reports.
        /// </summary>
        internal static string Reporting_Reports_Description {
            get {
                return ResourceManager.GetString("Reporting.Reports.Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --Reports.
        /// </summary>
        internal static string Reporting_Reports_Example {
            get {
                return ResourceManager.GetString("Reporting.Reports.Example", resourceCulture);
            }
        }
    }
}