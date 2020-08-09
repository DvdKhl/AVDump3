# AVDump3

## What does AVDump3 do
The main purpose of AVDump is to provide meta information about multi media files (Video, Audio, Subtitles and so on) and their file hashes by selectable report formats.

Though this is the main purpose, AVDump can be used for multiple other purposes. It basically reads data from a source and provides it to multiple consumers in parallel while the data is only read once and never copied.
So imaginable other uses would be to copy a file from once source to multiple destinations at the highest speed possible (bottlenecked by the slowest reader/writer) while at the same time calculate multiple hashes for it in one pass.

## Example Output

## What happened to AVDump1 & AVDump2

## Speed

## Cross-Platform

## Module System

## Commandline Arguments
For more detailed information please run AVD3 with `--Help`!

|Argument|Namespace|Description|Default|Example
|--|--|--|--|--
|--Recursive, -R|FileDiscovery|Recursively descent into Subdirectories|False|--Recursive
|--ProcessedLogPath, --PLPath|FileDiscovery|Appends the full filepath to the specified path|{}|--ProcessedLogPath=\<FilePath1>[:\<FilePath2>...]
|--SkipLogPath, --SLPath|FileDiscovery|Filepaths contained in the specified file will not be processed|{}|--SkipLogPath=\<FilePath1>[:\<FilePath2>...]
|--DoneLogPath, --DLPath|FileDiscovery|Will set --SkipLogPath and --ProcessedLogPath to the specified filepath||--DoneLogPath=\<Filepath>
|--WithExtensions, --WExts|FileDiscovery|Only/Don't Process files with selected Extensions||--WithExtensions=[-]\<Extension1>[,\<Extension2>...]
|--Concurrent, --Conc|FileDiscovery|Sets the maximal number of files which will be processed concurrently.<br>First param (max) sets a global limit. (path,max) pairs sets limits per path.|1|--Concurrent=\<max>[:\<path1>,\<max1>;\<path2>,\<max2>...]
|--ProducerMinReadLength|Processing||1|
|--ProducerMaxReadLength|Processing||8|
|--PrintAvailableSIMDs|Processing||False|
|--PauseBeforeExit, --PBExit|Processing|Pause console before exiting|False|--PauseBeforeExit
|--BufferLength, --BLength|Processing|Circular buffer size for hashing|64|--BufferLength=\<Size in MiB>
|--Consumers, --Cons|Processing|Select consumers to use. Use without arguments to list available consumers||--Consumers=\<ConsumerName1>[,\<ConsumerName2>...]
|--Test|FileMove||False|
|--LogPath|FileMove|||--FileMove.LogPath=\<FilePath>
|--Mode|FileMove||None|--FileMove.Mode=\<None\|PlaceholderInline\|PlaceholderFile\|CSharpScriptInline\|CSharpScriptFile\|DotNetAssembly>
|--Pattern|FileMove|Available Placeholders ${Name}:<br>FullName, FileName, FileExtension, FileNameWithoutExtension, DirectoryName, SuggestedExtension,<br>Hash-\<Name>-\<2\|4\|8\|10\|16\|32\|32Hex\|32Z\|36\|62\|64>-\<OC\|UC\|LC>|${DirectoryName}\${FileNameWithoutExtension}${FileExtension}|--FileMove.Pattern=${DirectoryName}\${FileNameWithoutExtension}${SuggestedExtension}
|--DisableFileMove|FileMove||False|--FileMove.DisableFileMove
|--DisableFileRename|FileMove||False|--FileMove.DisableFileRename
|--Replacements|FileMove|||--FileMove.Replacements=\<Match1>=\<Replacement1>[;\<Match2>=\<Replacement2>...]
|--PrintHashes|Reporting|Print calculated hashes in hexadecimal format to console|False|--PrintHashes
|--PrintReports|Reporting|Print generated reports to console|False|--PrintReports
|--Reports|Reporting|Select reports to use. Use without arguments to list available reports||--Reports
|--ReportDirectory, --RDir|Reporting|Reports will be saved to the specified directory|Current working directory|--ReportDirectory=\<Directory>
|--ReportFileName|Reporting|Reports will be saved/appended to the specified filename<br>The following placeholders ${Name} can be used: FileName, FileNameWithoutExtension, FileExtension, ReportName, ReportFileExtension|${FileName}.${ReportName}.${ReportFileExtension}|--ReportFileName=\<FileName>
|--ExtensionDifferencePath, --EDPath|Reporting|Logs the filepath if the detected extension does not match the actual extension||--EDPath=extdiff.txt
|--CRC32Error|Reporting|Searches the filename for the calculated CRC32 hash. If not present or different a line with the caluclated hash and the full path of the file is appended to the specified path<br>The regex pattern should contain the placeholder ${CRC32} which is replaced by the calculated hash prior matching.<br>Consumer CRC32 will be force enabled!|(, (?i)${CRC32})|--CRC32Error=\<Filepath>:\<RegexPattern>
|--SaveErrors|Diagnostics|Errors occuring during program execution will be saved to disk|False|--SaveErrors
|--SkipEnvironmentElement|Diagnostics|Skip the environment element in error files|False|--SkipEnvironmentElement
|--IncludePersonalData|Diagnostics|Various places may include personal data. Currently this only affects error files, which will then include the full filepath|False|--IncludePersonalData
|--PrintDiscoveredFiles|Diagnostics||False|
|--ErrorDirectory|Diagnostics|If --SaveErrors is specified the error files will be placed in the specified path|Current working directory|--ErrorDirectory=\<DirectoryPath>
|--NullStreamTest|Diagnostics|Use Memory as the DataSource for HashSpeed testing. Overrides any FileDiscovery Settings!|0:0:0|--NullStreamTest=\<StreamCount>:\<StreamLength in MiB>:\<ParallelStreamCount>
|--HideBuffers|Display|Hides buffer bars|False|--HideBuffers
|--HideFileProgress|Display|Hides file progress|False|--HideFileProgress
|--HideTotalProgress|Display|Hides total progress|False|--HideTotalProgress
|--ShowDisplayJitter|Display|Displays the time taken to calculate progression stats and drawing to console|False|--ShowDisplayJitter
|--ForwardConsoleCursorOnly|Display|The cursor position of the console will not be explicitly set. This option will disable most progress output|False|--ForwardConsoleCursorOnly

## Structure

### Projects

### Modules
Module Management

### Processing
The Processing Module contains the core functionality and is responsible for reading an passing the data to its consumers.
The Processing has been abstracted into multiple layers. The following interfaces and their description sketch their core responsibilities and are listed in dependency order, beginning with dependency free ones.

**IMirroredBuffer**:  
Provides reusable memory space to read data into. They are called *MirroredBuffer* because the used address space is *attached* again after the end of it. So, if A is the first address space and B the mirrored one, both A and B would point to the same physical memory in addition B starts directly after A. The importance if this is explained in the following paragraph.
The usual size for each *IMirroredBuffer* is around 16MiB to 64MiB.

**ICircularBuffer**:  
Can make use of an *IMirroredBuffer* instance. It handles a single writer data space and multiple reader data spaces, providing *views* into the address space of an *IMirroredBuffer* interface and methods to advance the reader/writer position, making sure a reader view doesn't overlap the writers view.
Once the end of the Buffer is reached it wraps around and starts reading/writing at the beginning of the Buffer again. This normally creates an issue for the writer/reader when data to be read/write needs to be wrapped around, making the implementation of a writer/reader more complex. The solution to avoid this problem is enabled by the *MirroredBuffer*, since a writer/reader can just write/read past the end of the buffer and transparently reach the start of the buffer. This way a writer/reader can always write/read their individual data lengths as long as the length is shorter than the buffer without being cut-off by the end of the buffer.

**IBlockSource**:  
Responsible for reading from a datasource which usually is a Filestream but can be any kind of datasource as long as it is capable of forward reading and has a fixed length (this requirement may be dropped later).

**IBlockStream**:  
Can make use of an *ICircularBuffer* and *IBlockSource* instance. It uses the *IBlockSource* instance to continuously write data to the *ICircularBuffer* instance while providing methods for readers to read data from the *ICircularBuffer* and providing progress information. It additionally synchronizes the writer and readers, blocking the writer if there is no space to write or blocking the reader if there is nothing to read.

**IBlockStreamReader**:  
Can make use of an *IBlockStream* instance and restricts the access to the *IBlockStream* instance allowing only access to one reader. In addition, it also provides hints to the reader how long their reads should/can be.

**IBlockConsumer** and **BlockConsumer**:  
Can make use of an *IBlockStreamReader* instance and uses it to consume data. Each BlockConsumer runs in its own thread (subject to change) and can operate on the data for its own purposes. It can request a minimum data length to be available and the called method will block until it can satisfy that request or until there is no data left.

**HashCalculator**:  
Derives from BlockConsumer and should be used to implement HashAlgorithm BlockConsumers. It takes an instance of *IAVDHashAlgorithm* an handles the reading and passing of data into the *IAVDHashAlgorithm* instance.
Please note that .NET Framework class HashAlgorithm is not supported because it has yet to be extended to provide transformation methods for Span<T>

**MatroskaParser**, **OggParser** and **MP4Parser**:  
Derives from BlockConsumer and can read their respective data structure. This is enabled by the BXmlLib Project. The read data is then interpreted and stored in multiple classes for later use (Information Module).

**IBlockConsumerFactory**:  
Creates instances of 'IBlockConsumer's and can be given a name. Each created *IBlockConsumer* is passed an instance of 'IBlockStreamReader'.

**IBlockConsumerSelector**:  
Can make use of multiple instances of *IBlockConsumerFactory*'s and provides the ability to select multiple *IBlockConsumerFactory*s based on its name and Stream to be processed.

**IMirroredBufferPool**:  
Stores instances of *IMirroredBuffer* and creates additional instances when necessary. Controls the size of the created Buffers.

**IStreamConsumer**:  
Can make use of an *IBlockStream* and multiple *IBlockConsumer*s. Once started it will create and start a thread for the *IBlockStream* instance (writer) and a thread for each *IBlockConsumer* instance. And kick off the writing and reading process blocking until finished, aggregating any exception that is thrown which is then thrown.

**IStreamConsumerFactory**:  
Can make use of *IMirroredBufferPool* and *IBlockConsumerSelector*. Creates an instance of *IStreamConsumer* and passes its necessary dependencies by renting an *IMirroredBuffer* instance and creating an instance of *ICircularBuffer*, *IBlockSource*, *IBlockStream* and multiple *IBlockConsumer* by using the *IBlockConsumerSelector* instance.

**IStreamProvider**:  
Provides an IEnumerable of Streams to be processed with cancellation support. It has the ability to control how many Streams are processed in parallel and in which order. This is used to control the maximum of parallel Streams and the maximum of parallel streams per base path (e.g. reading more than two files from one drive usually results in decreased throughput).

**IStreamConsumerCollection**:  
Can make use of *IStreamProvider* and *IStreamConsumerFactory*. It continously gets Streams from the *IStreamProvider* instance and hands of its processing to another thread. Parallel processing is only limited by the *IStreamProvider* instance blocking until the next stream is returned. For each stream an *IStreamConsumer* instance is created by using the *IStreamConsumerFactory* instance and started immediately afterwards.
Before each *IStreamConsumer* instance is started, an event is raised with which the responsible party can register with additional events for completion and exception handling. Also provides progress report and cancellation support.
Configuration of this instance is handled by the *AVD3ProcessingModule* instance.

### Settings

### Reporting

### Information

### Third Party Modules

## Contributing
