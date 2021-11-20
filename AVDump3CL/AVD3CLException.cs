using AVDump3Lib;

namespace AVDump3CL;

public class AVD3CLException : AVD3LibException {
	public AVD3CLException(string message, Exception innerException) : base(message, innerException) { }
	public AVD3CLException(string message) : base(message) { }
}
