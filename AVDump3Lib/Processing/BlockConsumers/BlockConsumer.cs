﻿using System;
using System.Threading;
using AVDump3Lib.BlockBuffers;

namespace AVDump3Lib.BlockConsumers {
	public interface IBlockConsumer {
		string Name { get; }
		Exception Exception { get; }
		void ProcessBlocks(CancellationToken ct);
		bool IsConsuming { get; }
	}


	public abstract class BlockConsumer : IBlockConsumer {
		protected IBlockStreamReader Reader { get; }

		public Exception Exception { get; private set; }

		public string Name { get; } //TODO

		public bool IsConsuming { get { return !Reader.DroppedOut; } }

		public BlockConsumer(string name, IBlockStreamReader reader) {
			Name = name;
			Reader = reader;
		}

		public void ProcessBlocks(CancellationToken ct) {
			try {
				DoWork(ct);
			} catch(Exception ex) {
				Exception = ex;
			} finally {
				Reader.DropOut();
			}
		}

		protected abstract void DoWork(CancellationToken ct);
	}
}
