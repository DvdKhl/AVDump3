using System;
using System.Threading;
using AVDump3Lib.BlockBuffers;

namespace AVDump3Lib.BlockConsumers {
    public interface IBlockConsumer {
		Exception Exception { get; }
		void ProcessBlocks(CancellationToken ct);
	}

	public abstract class BlockConsumer : IBlockConsumer {
		protected IBlockStreamReader Reader { get; }

		public Exception Exception { get; private set; }

		public BlockConsumer(IBlockStreamReader reader) {
			Reader = reader;
		}

        public void ProcessBlocks(CancellationToken ct) {
            try {
                DoWork(ct);
            } catch(Exception ex) {
                Exception = ex;
            } finally {
                Reader.DropOut(); ;
            }
        }

        protected abstract void DoWork(CancellationToken ct);
    }
}
