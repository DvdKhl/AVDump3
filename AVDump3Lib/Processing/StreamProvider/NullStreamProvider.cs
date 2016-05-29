using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.StreamProvider {
	public class NullStream : Stream {
		private long position;

		public override bool CanRead { get; } = true;
		public override bool CanSeek { get; } = false;
		public override bool CanWrite { get; } = false;
		public override long Length { get; } = 1L << 40;

		public override long Position {
			get { return position; }
			set { position = value; }
		}

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count) {
			position += count;
			return count;
		}

		public override long Seek(long offset, SeekOrigin origin) { return 0; }

		public override void SetLength(long value) { }

		public override void Write(byte[] buffer, int offset, int count) { }
	}

	public class NullProvidedStream : ProvidedStream {
		public NullProvidedStream(object tag, Stream stream) : base(tag, stream) { }
		public override void Dispose() { }
	}

	public class NullStreamProvider : IStreamProvider {
		public IEnumerable<ProvidedStream> GetConsumingEnumerable(CancellationToken ct) {
			yield return new NullProvidedStream("NULL", new NullStream());
		}
	}
}
