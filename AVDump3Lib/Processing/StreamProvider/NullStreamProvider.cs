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
        public override long Length { get; } 

        public override long Position {
            get { return position; }
            set { position = value; }
        }

        public override void Flush() { }

		public override int Read(Span<byte> buffer) {
			var bytesread = (int)Math.Min(buffer.Length, Length - position);
			position += bytesread;
			return bytesread;
		}
		public override int Read(byte[] buffer, int offset, int count) => Read(((Span<byte>)buffer).Slice(offset, count));

		public override long Seek(long offset, SeekOrigin origin) { return 0; }

        public override void SetLength(long value) { }

        public override void Write(byte[] buffer, int offset, int count) { }

        public NullStream(long length) {
            Length = length;
        }
    }

    public class NullProvidedStream : ProvidedStream {
        private SemaphoreSlim limiter;

        public NullProvidedStream(object tag, Stream stream, SemaphoreSlim limiter) : base(tag, stream) {
            this.limiter = limiter;
        }
        public override void Dispose() {
            limiter.Release();
        }
    }

    public class NullStreamProvider : IStreamProvider {
        private SemaphoreSlim limiter;

        public long StreamLength { get; }
        public int StreamCount { get; }
        public int ParallelStreamCount { get; }

        public NullStreamProvider(int streamCount, long streamLength, int parallelStreamCount) {
            StreamLength = streamLength;
            StreamCount = streamCount;
            ParallelStreamCount = parallelStreamCount;

            limiter = new SemaphoreSlim(parallelStreamCount);
        }

        public IEnumerable<ProvidedStream> GetConsumingEnumerable(CancellationToken ct) {
            for(int i = 0; i < StreamCount; i++) {
                limiter.Wait();
                yield return new NullProvidedStream("NULL" + i, new NullStream(StreamLength), limiter);
            }
        }
    }
}
