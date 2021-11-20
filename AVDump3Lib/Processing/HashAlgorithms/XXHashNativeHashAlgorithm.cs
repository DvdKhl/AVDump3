namespace AVDump3Lib.Processing.HashAlgorithms {


	//public enum XXHashLength { Bits32 = 32, Bits64 = 64, Bits128 = 128 };
	//public unsafe class XXHashNativeHashAlgorithm : AVDNativeHashAlgorithm {
	//	internal static class NativeMethods {
	//		[DllImport("AVDump3NativeLib")]
	//		internal static extern IntPtr XXHashCreate(ref int hashLength, out int blockSize);
	//		[DllImport("AVDump3NativeLib")]
	//		internal static extern void XXHashInit(IntPtr handle);
	//		[DllImport("AVDump3NativeLib")]
	//		internal static extern void XXHashTransform(IntPtr handle, byte* b, int length);
	//		[DllImport("AVDump3NativeLib")]
	//		internal static extern void XXHashFinal(IntPtr handle, byte* b, int length, byte* hash);
	//	}

	//	public XXHashNativeHashAlgorithm(XXHashLength hashLength) : this(hashLength, 0) { }
	//	public XXHashNativeHashAlgorithm(XXHashLength hashLength, ulong seed) : base(CreateWithSeed(seed), NativeMethods.XXHashInit, NativeMethods.XXHashTransform, NativeMethods.XXHashFinal, (int)hashLength) { } //TODO


	//	private static IntPtr CreateWithSeed(ulong seed) {

	//	}
	//}
}
