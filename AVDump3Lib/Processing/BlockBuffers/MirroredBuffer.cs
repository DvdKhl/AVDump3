using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AVDump3Lib.Processing.BlockBuffers {
    public interface IMirroredBuffer : IDisposable {
        int Length { get; }

        ReadOnlySpan<byte> ReadOnlySlice(int offset, int length);
        Span<byte> Slice(int offset, int length);
    }

    public unsafe class MirroredBufferWindows : IMirroredBuffer {
        #region WinAPI
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO {
            public ushort processorArchitecture;
            private ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }
        [DllImport("kernel32", EntryPoint = "GetSystemInfo", SetLastError = true)]
        private static extern void GetSystemInfo(out SYSTEM_INFO pSi);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFileMapping(
            [In] uint hFile,
            [In][Optional] IntPtr lpAttributes,
            [In] int flProtect,
            [In] int dwMaximumSizeHigh,
            [In] int dwMaximumSizeLow,
            [In][Optional] string lpName
        );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFileEx(
            [In] IntPtr hFileMappingObject,
            [In] int dwDesiredAccess,
            [In] int dwFileOffsetHigh,
            [In] int dwFileOffsetLow,
            [In] int dwNumberOfBytesToMap,
            [In][Optional] IntPtr lpBaseAddress
        );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool UnmapViewOfFile(
            [In] IntPtr lpBaseAddress
        );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(
            [In] IntPtr hObject
        );


        [Flags]
        public enum AllocationType {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }
        [Flags]
        public enum MemoryProtection {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAlloc(
            [In][Optional] IntPtr lpAddress,
            [In] UIntPtr dwSize,
            [In] AllocationType flAllocationType,
            [In] MemoryProtection flProtect
        );

        [Flags]
        public enum FreeType {
            Decommit = 0x4000,
            Release = 0x8000,
        }
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool VirtualFree(
            [In] IntPtr lpAddress,
            [In] UIntPtr dwSize,
            [In] FreeType flFreeType
        );


        private const uint INVALID_HANDLE = unchecked((uint)-1);
        #endregion

        private IntPtr Handle { get; }
        private byte* Data { get; }
        public int Length { get; }

        public MirroredBufferWindows(int length) {
            GetSystemInfo(out SYSTEM_INFO info);

            Length = (int)((length / info.allocationGranularity + (length % info.allocationGranularity == 0 ? 0U : 1U)) * info.allocationGranularity);
            Handle = CreateFileMapping(INVALID_HANDLE, IntPtr.Zero, 0x04, 0, Length, null);

            IntPtr data = IntPtr.Zero, mirror = IntPtr.Zero;
            for(int i = 0; i < 100; i++) {
                var memPtr = VirtualAlloc(IntPtr.Zero, (UIntPtr)(Length * 2), AllocationType.Reserve, MemoryProtection.ReadWrite);
                if(memPtr == IntPtr.Zero) continue;

                if(!VirtualFree(memPtr, UIntPtr.Zero, FreeType.Release)) throw new Exception("Addresspace leak");


                data = MapViewOfFileEx(Handle, 0x03, 0, 0, Length, IntPtr.Zero);
                if(data == IntPtr.Zero) continue;

                mirror = MapViewOfFileEx(Handle, 0x03, 0, 0, Length, data + Length);
                if(mirror == IntPtr.Zero) {
                    if(!UnmapViewOfFile(data)) throw new Exception();
                    continue;
                }
                break;
            }

            if(mirror == IntPtr.Zero) throw new Exception("Couldn't create Mirror Buffer");

            var d = (byte*)data.ToPointer();
            var m = (byte*)mirror.ToPointer();
            for(int i = 0; i < Length; i++) {
                *(m + i) = (byte)i;

                if(*(d + i) != *(m + i)) throw new Exception("Data Mirroring failed");
            }
            for(int i = 0; i < Length * 2; i++) *(d + i) = (byte)i;


            Data = (byte*)data;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadOnlySlice(int offset, int length) => new ReadOnlySpan<byte>(Data + offset, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> Slice(int offset, int length) => new Span<byte>(Data + offset, length);

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing) {
            if(!disposedValue) {
                if(disposing) {
                    //Handle = IntPtr.Zero;
                    //Data = (byte*)0;
                }
                if(!UnmapViewOfFile((IntPtr)Data)) throw new Exception();
                if(!UnmapViewOfFile((IntPtr)Data + Length)) throw new Exception();
                if(!CloseHandle(Handle)) throw new Exception();
                disposedValue = true;
            }
        }
        ~MirroredBufferWindows() { Dispose(false); }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        #endregion
    }
}
