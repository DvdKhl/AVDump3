using System;

namespace AVDump3Lib.Processing.HashAlgorithms {
    public interface IAVDHashAlgorithm : IDisposable {
        int BlockSize { get; }

        void Initialize();
        int TransformFullBlocks(ReadOnlySpan<byte> data);
        ReadOnlySpan<byte> TransformFinalBlock(ReadOnlySpan<byte> data);
    }


    public abstract class AVDHashAlgorithm : IAVDHashAlgorithm {
        public abstract int BlockSize { get; }

        public abstract void Initialize();
        public abstract ReadOnlySpan<byte> TransformFinalBlock(ReadOnlySpan<byte> data);
        public int TransformFullBlocks(ReadOnlySpan<byte> data) {
            var toProcess = data.Slice(0, (data.Length / BlockSize) * BlockSize);
            if(toProcess.Length > 0) HashCore(toProcess);
            return toProcess.Length;
        }

        protected abstract void HashCore(ReadOnlySpan<byte> data);


        #region IDisposable Support
        protected bool DisposedValue { get; private set; } = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!DisposedValue) DisposedValue = true;
        }

        ~AVDHashAlgorithm() => Dispose(false);
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
