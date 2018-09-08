# AVDump3

## What does AVDump3 do
The main purpose of AVDump is to provide meta information about multi media files (Video, Audio, Subtitles and so on) and their file hashes in choosable report formats.

Though this is the main purpose, AVDump can be used for multiple other purposes. It basically reads data from a source and provides it to multiple consumers in parallel while the data is only read once and never copied.
So imaginable other uses would be to copy a file from once source to multiple destinations at the highest speed possible (bottlenecked by the slowest reader/writer) while at the same time calculate multiple hashes for it in one pass.

## Example Output

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
Can make use of an *IBlockStreamReader* instance and uses it to consume data. Each BlockConsumer runs in its own thread (subject to change) and can operate on the data for its own purposes.

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
