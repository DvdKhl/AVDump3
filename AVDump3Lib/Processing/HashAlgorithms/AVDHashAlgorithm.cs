using System;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public interface IAVDHashAlgorithm : IDisposable {
		int BlockSize { get; }

		void Initialize();
		int TransformFullBlocks(in ReadOnlySpan<byte> data);
		ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data);
		ImmutableArray<ImmutableArray<byte>> AdditionalHashes => ImmutableArray<ImmutableArray<byte>>.Empty;
	}

	public abstract class AVDHashAlgorithm : IAVDHashAlgorithm {
		public abstract int BlockSize { get; }

		public void Initialize() {
			AdditionalHashes = ImmutableArray<ImmutableArray<byte>>.Empty;
			InitializeInternal();
		}
		protected abstract void InitializeInternal();

		public abstract ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data);

		public ImmutableArray<ImmutableArray<byte>> AdditionalHashes { get; protected set; } = ImmutableArray<ImmutableArray<byte>>.Empty;

		public int TransformFullBlocks(in ReadOnlySpan<byte> data) {
			var toProcess = data[..((data.Length / BlockSize) * BlockSize)];
			if(!toProcess.IsEmpty) HashCore(toProcess);
			return toProcess.Length;
		}

		protected abstract void HashCore(in ReadOnlySpan<byte> data);


		#region IDisposable Support
		protected bool DisposedValue { get; private set; } // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			if(!DisposedValue) DisposedValue = true;
		}

		~AVDHashAlgorithm() => Dispose(false);
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	public class AVDHashAlgorithmIncrmentalHashAdapter : AVDHashAlgorithm {
		public override int BlockSize { get; }
		public IncrementalHash Hash { get; }

		public AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName hashAlgorithmName, int blockSize) {
			Hash = IncrementalHash.CreateHash(hashAlgorithmName);
			BlockSize = blockSize;
		}

		protected override void InitializeInternal() => Hash.GetHashAndReset();
		public override ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data) {
			Hash.AppendData(data);
			return Hash.GetHashAndReset();
		}
		protected override void Dispose(bool disposing) {
			Hash.Dispose();
			base.Dispose(disposing);
		}

		protected override void HashCore(in ReadOnlySpan<byte> data) => Hash.AppendData(data);

	}
}
