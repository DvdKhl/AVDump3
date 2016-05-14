using System;
using System.Linq;
using AVDump3Lib.Processing.StreamConsumer;
using System.Threading.Tasks;
using System.Threading;
using AVDump3Lib.BlockConsumers;
using AVDump2Lib.InfoProvider;
using AVDump2Lib.InfoGathering.InfoProvider;
using AVDump3Lib.BlockConsumers.Matroska;
using System.Xml.Linq;
using AVDump3Lib.Information.MetaInfo.Media;
using AVDump3Lib.Information.MetaInfo;
using System.Globalization;
using System.IO;
using AVDump2Lib.InfoProvider.Tools;
using AVDump3Lib.Misc;
using System.Reflection;
using AVDump3Lib.BlockBuffers;
using System.Collections.Generic;
using AVDump3Lib.Processing.StreamProvider;

namespace AVDump3CL {
	public class BytesReadProgress : IBytesReadProgress {
		private long bytesProcessed;
		private Dictionary<IBlockStream, StreamConsumerProgressPair> blockStreamProgress;

		private class StreamConsumerProgressPair {
			public ProvidedStream ProvidedStream;
			public IStreamConsumer StreamConsumer;
			public long[] BytesRead;

			public StreamConsumerProgressPair(ProvidedStream providedStream, IStreamConsumer streamConsumer) {
				ProvidedStream = providedStream;
				StreamConsumer = streamConsumer;
				BytesRead = new long[streamConsumer.BlockConsumers.Count + 1];
			}
		}

		public class FileProgress {
			public string FilePath { get; private set; }
			public long FileLength { get; private set; }
			public long BytesProcessed { get; private set; }
			public SortedDictionary<string, long> BytesProcessedPerBlockConsumer { get; private set; }
		}

		public class Progress {
			public long TotalBytesProcessed { get; private set; }
			public IReadOnlyCollection<FileProgress> FileProgressCollection { get; private set; }
		}

		public BytesReadProgress() {
			blockStreamProgress = new Dictionary<IBlockStream, StreamConsumerProgressPair>();
		}

		public void Report(BlockStreamProgress value) {
			StreamConsumerProgressPair streamConsumerProgressPair;
			lock(blockStreamProgress) {
				streamConsumerProgressPair = blockStreamProgress[value.Sender];
			}

			Interlocked.Add(ref streamConsumerProgressPair.BytesRead[value.Index + 1], value.BytesRead);
		}

		public void Register(ProvidedStream providedStream, IStreamConsumer streamConsumer) {
			lock(blockStreamProgress) {
				blockStreamProgress.Add(streamConsumer.BlockStream, new StreamConsumerProgressPair(providedStream, streamConsumer));
			}

			streamConsumer.Finished += s => {
				lock(blockStreamProgress) {
					blockStreamProgress.Remove(streamConsumer.BlockStream);
				}

				if(s.RanToCompletion) {
					Interlocked.Add(ref bytesProcessed, s.BlockStream.Length);
				}
			};
		}
	}

	public class AVD3CL {
		private StreamConsumerCollection streamConsumerCollection;

		public bool UseNtfsAlternateStreams { get; set; }

		public AVD3CL(StreamConsumerCollection streamConsumerCollection) {
			this.streamConsumerCollection = streamConsumerCollection;
		}

		public void Process() {
			streamConsumerCollection.ConsumingStream += ConsumingStream; ;

			var consumeTask = Task.Run(() =>
				streamConsumerCollection.ConsumeStreams(CancellationToken.None)
			);

			var startedOn = DateTime.UtcNow.AddSeconds(-1);
			while(!consumeTask.IsCompleted) {
				lock(streamConsumerCollection) {
					Console.CursorLeft = 0;
					Console.Write("{0}GB {1}s {2}MB/s  ",
						streamConsumerCollection.ReadBytes >> 30,
						(int)(DateTime.UtcNow - startedOn).TotalSeconds,
						(streamConsumerCollection.ReadBytes >> 20) / (int)(DateTime.UtcNow - startedOn).TotalSeconds);
				}

				Thread.Sleep(200);
			}

			Console.Read();
		}

		private async void ConsumingStream(object sender, ConsumingStreamEventArgs e) {
			e.OnException += (s, args) => {
				args.IsHandled = true;
				args.Retry = args.RetryCount < 2;
			};

			var blockConsumers = await e.FinishedProcessing;
			var hashes = blockConsumers.OfType<HashCalculator>().Select(h =>
				h.HashAlgorithm.GetType().Name + ": " + BitConverter.ToString(h.HashAlgorithm.Hash).Replace("-", "")
			);

			if(UseNtfsAlternateStreams) {
				using(var altStreamHandle = NtfsAlternateStreams.SafeCreateFile(
					NtfsAlternateStreams.BuildStreamPath((string)e.Tag, "AVDump3.xml"),
					NtfsAlternateStreams.ToNative(FileAccess.ReadWrite), FileShare.None,
					IntPtr.Zero, FileMode.OpenOrCreate, 0, IntPtr.Zero))
				using(var altStream = new FileStream(altStreamHandle, FileAccess.ReadWrite)) {
					var avd3Elem = new XElement("AVDump3",
					  new XElement("Revision",
						new XAttribute("Build", Assembly.GetExecutingAssembly().GetName().Version.Build),
						blockConsumers.OfType<HashCalculator>().Select(hc =>
						  new XElement(hc.HashAlgorithmType.Key, BitConverter.ToString(hc.HashAlgorithm.Hash).Replace("-", ""))
						)
					  )
					);
					avd3Elem.Save(altStream, SaveOptions.None);
				}
			}



			var infoProvider = new CompositeMediaProvider(
				new HashProvider(blockConsumers.OfType<HashCalculator>().Select(b =>
					new HashProvider.HashResult(b.HashAlgorithmType, b.HashAlgorithm.Hash)
				)),
				new MediaInfoLibProvider((string)e.Tag),
				new MatroskaProvider(blockConsumers.OfType<MatroskaParser>().First().Info)
			);

			if(!Directory.Exists("Dumps")) Directory.CreateDirectory("Dumps");
			GenerateAVDump3Report(infoProvider).Save("Dumps\\" + Path.GetFileName((string)e.Tag) + ".xml");


			lock(sender) {
				Console.CursorLeft = 0;

				Console.WriteLine(e.Tag);
				foreach(var hash in hashes) {
					Console.WriteLine(hash);
				}
				Console.WriteLine();
			}
		}

		public static XElement GenerateAVDump3Report(MediaProvider provider) {
			var root = new XElement("File");
			GenerateAVDump3ReportSub(root, provider);
			return root;
		}
		public static void GenerateAVDump3ReportSub(XElement elem, MetaInfoContainer container) {
			foreach(var item in container.Items) {
				if(item.Value == null) continue;

				var subElem = new XElement(item.Type.Key);
				if(item.Value is MetaInfoContainer) {
					GenerateAVDump3ReportSub(subElem, (MetaInfoContainer)item.Value);

				} else if(item.Provider is HashProvider) {
					var bVal = (byte[])item.Value;
					subElem.Value = BitConverter.ToString(bVal).Replace("-", "");

				} else if(item.Value is byte[]) {
					var bVal = (byte[])item.Value;
					subElem.Value = bVal.Length <= 16 ? BitConverter.ToString(bVal) : "Byte[" + bVal.Length + "]";

				} else if(item.Value is IFormattable) subElem.Value = ((IFormattable)item.Value).ToString(null, CultureInfo.InvariantCulture);
				else subElem.Value = item.Value.ToString();

				subElem.Add(new XAttribute("p", item.Provider.Name));

				if(item.Type.Unit != null) {
					subElem.Add(new XAttribute("u", item.Type.Unit));
				}


				elem.Add(subElem);
			}
		}

	}
}
