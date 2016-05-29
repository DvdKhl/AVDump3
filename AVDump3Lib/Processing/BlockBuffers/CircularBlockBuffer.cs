using System;
using System.Linq;

namespace AVDump3Lib.Processing.BlockBuffers {
    public class CircularBlockBuffer {
        public byte[][] Blocks{ get { return blocks; } }
        private readonly byte[][] blocks;

        private int producer;
        private int[] consumers;

        public int BlockLength { get { return blocks[0].Length; } }

        public CircularBlockBuffer(byte[][] blocks) {
            if(blocks == null) throw new ArgumentNullException(nameof(blocks));
            if(blocks.Length <= 1) throw new ArgumentException("Needs to have at least 2 elements", nameof(blocks));
            if(!blocks.All(b => blocks[0].Length == b.Length)) throw new ArgumentException("Items need have same length", nameof(blocks));
            if(blocks[0].Length == 0) throw new ArgumentException("Items cannot have a length of 0", nameof(blocks));

            this.blocks = blocks;

        }

		public void SetConsumerCount(int count) {
			if(count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Needs to be greater than 0");
			consumers = new int[count];
		}

		public void ConsumerDropOut(int consumerIndex) { consumers[consumerIndex] = int.MaxValue; }
        public bool ConsumerCanRead(int consumerIndex) { return consumers[consumerIndex] < producer; }
        public void ConsumerAdvance(int consumerIndex) { consumers[consumerIndex]++; }
		public int ConsumerBlocksRead(int consumerIndex) { return consumers[consumerIndex]; }
        public byte[] ConsumerBlock(int consumerIndex) { return blocks[consumers[consumerIndex] % blocks.Length]; }

        public bool ProducerCanWrite() { foreach(var consumer in consumers) if(consumer + blocks.Length == producer) return false; return true; }
        public void ProducerAdvance() { producer++; }
        public byte[] ProducerBlock() { return blocks[producer % blocks.Length]; }

        public bool IsEmpty() { foreach(var consumer in consumers) if(consumer < producer) return false; return true; }
	}
}
