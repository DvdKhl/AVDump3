using System;
using System.Linq;
using System.Threading;

namespace AVDump3Lib.Processing.BlockBuffers {
    public interface ICircularBuffer {
        int Length { get; }
        bool IsCompleted { get; }
        bool IsProducionCompleted { get; }

        void ConsumerAdvance(int consumerId, int length);
        ReadOnlySpan<byte> ConsumerBlock(int consumerId);
        void CompleteConsumption(int consumerId);
        bool ConsumerCompleted(int consumerId);

        void ProducerAdvance(int length);
        Span<byte> ProducerBlock();
        void CompleteProduction();
    }


    public class CircularBuffer64Bit : ICircularBuffer {
        private readonly IMirroredBuffer buffer;
        private readonly long[] consumers;
        private long producer;

        public long ConsumerBytesRead(int consumerId) => consumers[consumerId];

        public int Length => buffer.Length;

        public bool IsCompleted {
            get {
                if(!IsProducionCompleted) return false;
                foreach(var consumer in consumers) if(consumer < producer) return false;
                return true;
            }
        }
        public bool IsProducionCompleted { get; private set; }


        public CircularBuffer64Bit(IMirroredBuffer buffer, int consumerCount) {
            this.buffer = buffer;
            consumers = new long[consumerCount];

        }

        public void CompleteConsumption(int consumerId) => consumers[consumerId] = long.MaxValue;
        public void ConsumerAdvance(int consumerId, int length) => consumers[consumerId] += length;
        public ReadOnlySpan<byte> ConsumerBlock(int consumerId) =>
            buffer.ReadOnlySlice(
                (int)(consumers[consumerId] % buffer.Length),
                (int)(producer - consumers[consumerId])
            );
        public bool ConsumerCompleted(int consumerId) {
            if(!IsProducionCompleted) return false;
            return consumers[consumerId] < producer;
        }

        private int ProducerCanWrite() {
            long lastConsumer = 0;
            foreach(var consumer in consumers) {
                if(lastConsumer < consumer) lastConsumer = consumer;
            }
            return buffer.Length - (int)(producer - lastConsumer);
        }
        public void ProducerAdvance(int length) => producer += length;
        public Span<byte> ProducerBlock() => buffer.Slice((int)(producer % buffer.Length), ProducerCanWrite());
        public void CompleteProduction() => IsProducionCompleted = true;
    }

    public class CircularBuffer32Bit : ICircularBuffer {
        private readonly MirroredBufferWindows buffer;
        private readonly long[] consumers;
        private long producer;

        public long ConsumerBytesRead(int consumerId) => Interlocked.Read(ref consumers[consumerId]);

        public int Length => buffer.Length;
        public bool IsCompleted {
            get {
                if(!IsProducionCompleted) return false;
                var localProducer = Interlocked.Read(ref producer);
                for(int i = 0; i < consumers.Length; i++) {
                    if(Interlocked.Read(ref consumers[i]) < localProducer) return false;
                }
                return true;
            }
        }
        public bool IsProducionCompleted { get; private set; }

        public CircularBuffer32Bit(MirroredBufferWindows buffer, int consumerCount) {
            this.buffer = buffer;
            consumers = new long[consumerCount];
        }

        public void CompleteConsumption(int consumerId) => Interlocked.Exchange(ref consumers[consumerId], long.MaxValue);
        public void ConsumerAdvance(int consumerId, int length) => Interlocked.Add(ref consumers[consumerId], length);
        public ReadOnlySpan<byte> ConsumerBlock(int consumerId) =>
            buffer.ReadOnlySlice(
                (int)(Interlocked.Read(ref consumers[consumerId]) % buffer.Length),
                (int)(Interlocked.Read(ref producer) - Interlocked.Read(ref consumers[consumerId]))
            );
        public bool ConsumerCompleted(int consumerId) {
            if(!IsProducionCompleted) return false;
            return Interlocked.Read(ref consumers[consumerId]) < Interlocked.Read(ref producer);
        }

        private int ProducerCanWrite() {
            long lastConsumer = 0;
            for(int i = 0; i < consumers.Length; i++) {
                var localConsumer = Interlocked.Read(ref consumers[i]);
                if(lastConsumer < localConsumer) lastConsumer = localConsumer;
            }
            return buffer.Length - (int)(Interlocked.Read(ref producer) - lastConsumer);
        }
        public void ProducerAdvance(int length) => Interlocked.Add(ref producer, length);
        public Span<byte> ProducerBlock() => buffer.Slice((int)(Interlocked.Read(ref producer) % buffer.Length), ProducerCanWrite());
        public void CompleteProduction() => IsProducionCompleted = true;

    }
}
