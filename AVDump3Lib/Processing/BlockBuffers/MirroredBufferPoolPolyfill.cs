using System.Runtime.CompilerServices;

namespace AVDump3Lib.Processing.BlockBuffers;

public class MirroredBufferPoolPolyfill(int bufferSize) : IMirroredBufferPool {
	private readonly Queue<IMirroredBuffer> slots = new();

	public int BufferSize { get; } = bufferSize;

	public IMirroredBuffer Take() {
		lock(slots) {
			if(slots.TryDequeue(out var buffer)) return buffer;
			return new MirroredBufferPolyfill(BufferSize);
		}
	}
	public void Release(IMirroredBuffer buffer) {
		lock(slots) slots.Enqueue(buffer);
	}


	private class MirroredBufferPolyfill(int length) : IMirroredBuffer {
		public int Length { get; } = length;


		private readonly byte[] data = new byte[length];

		public void Dispose() { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlySpan<byte> ReadOnlySlice(int offset, int length) => Slice(offset, length);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<byte> Slice(int offset, int length) {
			offset %= data.Length;

			if(offset < 0 || length < 0) throw new ArgumentOutOfRangeException();
			if(offset + length > data.Length) {
				var dataWrapAround = new byte[length];
				var firstPartLength = data.Length - offset;
				Buffer.BlockCopy(data, offset, dataWrapAround, 0, firstPartLength);
				Buffer.BlockCopy(data, 0, dataWrapAround, firstPartLength, length - firstPartLength);
				return dataWrapAround;
			} else {
				return data.AsSpan(offset, length);
			}
		}
	}

}