using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockBuffers;

public interface IMirroredBuffer : IDisposable {
	int Length { get; }

	ReadOnlySpan<byte> ReadOnlySlice(int offset, int length);
	Span<byte> Slice(int offset, int length);
}

public unsafe class MirroredBuffer : IMirroredBuffer {
	#region NativeApi
	[StructLayout(LayoutKind.Sequential)]
	private struct AVD3MirrorBufferCreateHandle {
		public IntPtr fileHandle;
		public IntPtr baseAddress;
		public int length;
	}
	[DllImport("AVDump3NativeLib")]
	private static extern IntPtr CreateMirrorBuffer(int minLength, out AVD3MirrorBufferCreateHandle handle);

	[DllImport("AVDump3NativeLib")]
	private static extern IntPtr FreeMirrorBuffer(ref AVD3MirrorBufferCreateHandle handle);
	#endregion

	private AVD3MirrorBufferCreateHandle handle;
	private byte* Data { get; }
	public int Length { get; }

	public MirroredBuffer(int length) {
		var result = Marshal.PtrToStringAnsi(CreateMirrorBuffer(length, out handle));
		if(!string.IsNullOrEmpty(result)) {
			throw new AVD3ForceMajeureException("Consider processing fewer files in parallel or reduce the buffer per file.", result);
		}

		Data = (byte*)handle.baseAddress;
		Length = handle.length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<byte> ReadOnlySlice(int offset, int length) => new(Data + offset, length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<byte> Slice(int offset, int length) => new(Data + offset, length);

	#region IDisposable Support
	private bool disposedValue = false;
	protected virtual void Dispose(bool disposing) {
		if(!disposedValue) {
			if(disposing) {
				//Handle = IntPtr.Zero;
				//Data = (byte*)0;
			}
			FreeMirrorBuffer(ref handle);
			disposedValue = true;
		}
	}
	~MirroredBuffer() { Dispose(false); }
	public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
	#endregion
}
