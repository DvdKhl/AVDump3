using System;
using System.Linq;

namespace AVDump3Lib.BlockBuffers {
    public class CircularBlockBuffer {
        public byte[][] Blocks{ get { return blocks; } }
        private readonly byte[][] blocks;

        private long producer;
        private readonly long[] consumers;

        public int BlockLength { get { return blocks[0].Length; } }

        public CircularBlockBuffer(byte[][] blocks, int consumerCount) {
            if(blocks == null) throw new ArgumentNullException(nameof(blocks));
            if(blocks.Length <= 1) throw new ArgumentException("Needs to have at least 2 elements", nameof(blocks));
            if(consumerCount <= 0) throw new ArgumentOutOfRangeException(nameof(consumerCount), "Needs to be greater than 0");
            if(!blocks.All(b => blocks[0].Length == b.Length)) throw new ArgumentException("Items need have same length", nameof(blocks));
            if(blocks[0].Length == 0) throw new ArgumentException("Items cannot have a length of 0", nameof(blocks));

            this.blocks = blocks;

            consumers = new long[consumerCount];
        }

        public void ConsumerDropOut(int consumerIndex) { consumers[consumerIndex] = long.MaxValue; }
        public bool ConsumerCanRead(int consumerIndex) { return consumers[consumerIndex] < producer; }
        public void ConsumerAdvance(int consumerIndex) { consumers[consumerIndex]++; }
        public byte[] ConsumerBlock(int consumerIndex) { return blocks[consumers[consumerIndex] % blocks.Length]; }

        public bool ProducerCanWrite() { foreach(var consumer in consumers) if(consumer + blocks.Length == producer) return false; return true; }
        public void ProducerAdvance() { producer++; }
        public byte[] ProducerBlock() { return blocks[producer % blocks.Length]; }

        public bool IsEmpty() { foreach(var consumer in consumers) if(consumer < producer) return false; return true; }
	}
}
