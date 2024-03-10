using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.HashAlgorithms;

public unsafe class AVDNativeHashAlgorithm : AVDHashAlgorithm {
	private static class NativeMethods {
		[DllImport("AVDump3NativeLib")]
		internal static extern void FreeHashObject(IntPtr handle);

	}


	protected delegate IntPtr CreateHandler(ref int hashLength, out int blockSize);
	protected delegate void InitHandler(IntPtr handle);
	protected delegate void TransformHandler(IntPtr handle, byte* b, int length);
	protected delegate void FinalHandler(IntPtr handle, byte* b, int length, byte* hash);


	private readonly IntPtr handle;
	private readonly InitHandler init;
	private readonly TransformHandler transform;
	private readonly FinalHandler final;

	protected AVDNativeHashAlgorithm(CreateHandler create, InitHandler init, TransformHandler transform, FinalHandler final, int hashBitCount) {
		if(create is null) throw new ArgumentNullException(nameof(create));
		handle = create(ref hashBitCount, out var blockSize);

		if(hashBitCount == 0) {
			throw new ArgumentNullException(nameof(hashBitCount), $"Hashlength {hashBitCount} not supported");
		}

		this.init = init ?? throw new ArgumentNullException(nameof(init));
		this.transform = transform ?? throw new ArgumentNullException(nameof(transform));
		this.final = final ?? throw new ArgumentNullException(nameof(final));

		BlockSize = blockSize;
		HashLengthInBits = hashBitCount;
	}

	protected AVDNativeHashAlgorithm(IntPtr handle, InitHandler init, TransformHandler transform, FinalHandler final, int blockSize, int hashBitCount) {
		this.init = init ?? throw new ArgumentNullException(nameof(init));
		this.transform = transform ?? throw new ArgumentNullException(nameof(transform));
		this.final = final ?? throw new ArgumentNullException(nameof(final));

		this.handle = handle;
		BlockSize = blockSize;
		HashLengthInBits = hashBitCount;
	}

	public override int BlockSize { get; }
	public int HashLengthInBits { get; }

	protected override void InitializeInternal() => init(handle);

	protected override unsafe void HashCore(in ReadOnlySpan<byte> data) {
		fixed(byte* bPtr = &data[0]) {
			transform(handle, bPtr, data.Length);
		}
	}
	public override ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data) {
		var hash = new byte[HashLengthInBits / 8];
		fixed(byte* hashPtr = hash, bPtr = data) {
			final(handle, bPtr, data.Length, hashPtr);
		}
		return hash;
	}

	protected override void Dispose(bool disposing) {
		if(!DisposedValue) NativeMethods.FreeHashObject(handle);
		base.Dispose(disposing);
	}

    public ReadOnlySpan<byte> ComputeHash(in ReadOnlySpan<byte> data) {
        var bytesProcessed = TransformFullBlocks(data);

        //return TransformFinalBlock(data[bytesProcessed..]); 

        var remainingData = data[bytesProcessed..];
        var hash = new byte[HashLengthInBits / 8];
        fixed(byte* hashPtr = hash, bPtr = remainingData) {
            final(handle, bPtr, remainingData.Length, hashPtr);
        }
        return hash;

    }
}
