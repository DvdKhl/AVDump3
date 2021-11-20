using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AVDump3Lib.Misc {
	public static class NtfsAlternateStreamsNativeMethods {
		#region Constants and flags

		public const int MaxPath = 256;
		private const string LongPathPrefix = @"\\?\";
		public const char StreamSeparator = ':';
		public const int DefaultBufferSize = 0x1000;

		private const int ErrorFileNotFound = 2;

		// "Characters whose integer representations are in the range from 1 through 31, 
		// except for alternate streams where these characters are allowed"
		// http://msdn.microsoft.com/en-us/library/aa365247(v=VS.85).aspx
		//private static readonly char[] InvalidStreamNameChars = Path.GetInvalidFileNameChars().Where(c => c < 1 || c > 31).ToArray();

		[Flags]
		public enum NativeFileFlags : uint {
			WriteThrough = 0x80000000,
			Overlapped = 0x40000000,
			NoBuffering = 0x20000000,
			RandomAccess = 0x10000000,
			SequentialScan = 0x8000000,
			DeleteOnClose = 0x4000000,
			BackupSemantics = 0x2000000,
			PosixSemantics = 0x1000000,
			OpenReparsePoint = 0x200000,
			OpenNoRecall = 0x100000
		}

		[Flags]
		public enum NativeFileAccess : uint {
			GenericRead = 0x80000000,
			GenericWrite = 0x40000000
		}

		#endregion


		#region P/Invoke Methods

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
		private static extern int FormatMessage(
			int dwFlags,
			IntPtr lpSource,
			int dwMessageId,
			int dwLanguageId,
			StringBuilder lpBuffer,
			int nSize,
			IntPtr vaListArguments);


		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetFileSizeEx(SafeFileHandle handle, out long size);

		[DllImport("kernel32.dll")]
		private static extern int GetFileType(SafeFileHandle handle);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern SafeFileHandle CreateFile(
			string name,
			NativeFileAccess access,
			FileShare share,
			IntPtr security,
			FileMode mode,
			NativeFileFlags flags,
			IntPtr template);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteFile(string name);

		#endregion

		#region Utility Methods

		private static int MakeHRFromErrorCode(int errorCode) {
			return (-2147024896 | errorCode);
		}

		private static string GetErrorMessage(int errorCode) {
			var lpBuffer = new StringBuilder(0x200);
			if(0 != FormatMessage(0x3200, IntPtr.Zero, errorCode, 0, lpBuffer, lpBuffer.Capacity, IntPtr.Zero)) {
				return lpBuffer.ToString();
			}

			return "UnknownError: " + errorCode;
		}

		private static void ThrowIOError(int errorCode, string path) {
			switch(errorCode) {
				case 0: {
						break;
					}
				case 2: // File not found
				{
						if(string.IsNullOrEmpty(path)) throw new FileNotFoundException();
						throw new FileNotFoundException(null, path);
					}
				case 3: // Directory not found
				{
						if(string.IsNullOrEmpty(path)) throw new DirectoryNotFoundException();
						throw new DirectoryNotFoundException("DirectoryNotFound: " + path);
					}
				case 5: // Access denied
				{
						if(string.IsNullOrEmpty(path)) throw new UnauthorizedAccessException();
						throw new UnauthorizedAccessException("AccessDenied_Path: " + path);
					}
				case 15: // Drive not found
				{
						if(string.IsNullOrEmpty(path)) throw new DriveNotFoundException();
						throw new DriveNotFoundException("DriveNotFound: " + path);
					}
				case 32: // Sharing violation
				{
						if(string.IsNullOrEmpty(path)) throw new IOException(GetErrorMessage(errorCode), MakeHRFromErrorCode(errorCode));
						throw new IOException("SharingViolation: " + path, MakeHRFromErrorCode(errorCode));
					}
				case 80: // File already exists
				{
						if(!string.IsNullOrEmpty(path)) {
							throw new IOException("FileAlreadyExists: " + path, MakeHRFromErrorCode(errorCode));
						}
						break;
					}
				case 87: // Invalid parameter
				{
						throw new IOException(GetErrorMessage(errorCode), MakeHRFromErrorCode(errorCode));
					}
				case 183: // File or directory already exists
				{
						if(!string.IsNullOrEmpty(path)) {
							throw new IOException("AlreadyExists: " + path, MakeHRFromErrorCode(errorCode));
						}
						break;
					}
				case 206: // Path too long
				{
						throw new PathTooLongException();
					}
				case 995: // Operation cancelled
				{
						throw new OperationCanceledException();
					}
				default: {
						Marshal.ThrowExceptionForHR(MakeHRFromErrorCode(errorCode));
						break;
					}
			}
		}

		public static void ThrowLastIOError(string path) {
			var errorCode = Marshal.GetLastWin32Error();
			if(0 != errorCode) {
				var hr = Marshal.GetHRForLastWin32Error();
				if(0 <= hr) throw new Win32Exception(errorCode);
				ThrowIOError(errorCode, path);
			}
		}

		public static NativeFileAccess ToNative(this FileAccess access) {
			NativeFileAccess result = 0;
			if(FileAccess.Read == (FileAccess.Read & access)) result |= NativeFileAccess.GenericRead;
			if(FileAccess.Write == (FileAccess.Write & access)) result |= NativeFileAccess.GenericWrite;
			return result;
		}

		public static string BuildStreamPath(string filePath, string streamName) {
			var result = filePath;
			if(!string.IsNullOrEmpty(filePath)) {
				if(1 == result.Length) result = ".\\" + result;
				result += StreamSeparator + streamName + StreamSeparator + "$DATA";
				if(MaxPath <= result.Length) result = LongPathPrefix + result;
			}
			return result;
		}


		public static bool SafeDeleteFile(string name) {
			if(string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

			var result = DeleteFile(name);
			if(!result) {
				var errorCode = Marshal.GetLastWin32Error();
				if(ErrorFileNotFound != errorCode) ThrowLastIOError(name);
			}

			return result;
		}

		public static SafeFileHandle SafeCreateFile(string path, NativeFileAccess access, FileShare share, IntPtr security, FileMode mode, NativeFileFlags flags, IntPtr template) {
			var result = CreateFile(path, access, share, security, mode, flags, template);
			if(!result.IsInvalid && 1 != GetFileType(result)) {
				result.Dispose();
				throw new NotSupportedException("NonFile: " + path);
			}

			return result;
		}

		private static long GetFileSize(string path, SafeFileHandle handle) {
			var result = 0L;
			if(null != handle && !handle.IsInvalid) {
				if(GetFileSizeEx(handle, out long value)) {
					result = value;
				} else {
					ThrowLastIOError(path);
				}
			}

			return result;
		}

		public static long GetFileSize(string path) {
			var result = 0L;
			if(!string.IsNullOrEmpty(path)) {
				using var handle = SafeCreateFile(path, NativeFileAccess.GenericRead, FileShare.Read, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
				result = GetFileSize(path, handle);
			}

			return result;
		}
		#endregion
	}
}
