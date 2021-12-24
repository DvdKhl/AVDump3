using AVDump3Lib;

namespace AVDump3Lib.UI;

public class AVD3UIException : AVD3LibException {
	public AVD3UIException(string message, Exception innerException) : base(message, innerException) { }
	public AVD3UIException(string message) : base(message) { }
}
