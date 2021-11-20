using AVDump3Lib.Processing.StreamConsumer;
using System;

namespace AVDump3Lib.Processing;

public class BlockConsumerFilterEventArgs : EventArgs {
	public IStreamConsumerCollection StreamConsumerCollection { get; }
	public string BlockConsumerName { get; }

	public bool Accepted { get; private set; }
	public void Accept() { Accepted = true; }

	public BlockConsumerFilterEventArgs(IStreamConsumerCollection streamConsumerCollection, string blockConsumerName) {
		StreamConsumerCollection = streamConsumerCollection;
		BlockConsumerName = blockConsumerName;
	}
}
