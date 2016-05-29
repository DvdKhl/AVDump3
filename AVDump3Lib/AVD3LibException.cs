using System;
using System.Runtime.Serialization;

namespace AVDump3Lib {
    [Serializable]
	public class AVD3LibException : Exception {
		public AVD3LibException() { }
		public AVD3LibException(string message) : base(message) { }
		public AVD3LibException(string message, Exception inner) : base(message, inner) { }
		protected AVD3LibException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
