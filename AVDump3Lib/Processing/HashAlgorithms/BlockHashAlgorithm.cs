using System;
using System.Security.Cryptography;

namespace AVDump3Lib.Processing.HashAlgorithms {
    public abstract class BlockHashAlgorithm : HashAlgorithm {
		protected byte[] ba_PartialBlockBuffer;
		protected int i_PartialBlockFill;

		protected int i_InputBlockSize;
		protected long l_TotalBytesProcessed;


		/// <summary>Initializes a new instance of the BlockHashAlgorithm class.</summary>
		/// <param name="blockSize">The size in bytes of an individual block.</param>
		protected BlockHashAlgorithm(int blockSize, int hashSize)
			: base() {
			this.i_InputBlockSize = blockSize;
			this.HashSizeValue = hashSize;
			ba_PartialBlockBuffer = new byte[BlockSize];
		}


		/// <summary>Initializes the algorithm.</summary>
		/// <remarks>If this function is overriden in a derived class, the new function should call back to
		/// this function or you could risk garbage being carried over from one calculation to the next.</remarks>
		public override void Initialize() {	//abstract: base.Initialize();
			l_TotalBytesProcessed = 0;
			i_PartialBlockFill = 0;
			if(ba_PartialBlockBuffer == null) ba_PartialBlockBuffer = new byte[BlockSize];
		}


		/// <summary>The size in bytes of an individual block.</summary>
		public int BlockSize { get { return i_InputBlockSize; } }

		/// <summary>The number of bytes currently in the buffer waiting to be processed.</summary>
		public int BufferFill { get { return i_PartialBlockFill; } }


		/// <summary>Performs the hash algorithm on the data provided.</summary>
		/// <param name="array">The array containing the data.</param>
		/// <param name="ibStart">The position in the array to begin reading from.</param>
		/// <param name="cbSize">How many bytes in the array to read.</param>
		protected override void HashCore(byte[] array, int ibStart, int cbSize) {
			int i;

			// Use what may already be in the buffer.
			if(BufferFill > 0) {
				if(cbSize + BufferFill < BlockSize) {
					// Still don't have enough for a full block, just store it.
					Buffer.BlockCopy(array, ibStart, ba_PartialBlockBuffer, BufferFill, cbSize);
					i_PartialBlockFill += cbSize;
					return;
				} else {
					// Fill out the buffer to make a full block, and then process it.
					i = BlockSize - BufferFill;
					Array.Copy(array, ibStart, ba_PartialBlockBuffer, BufferFill, i);
					ProcessBlock(ba_PartialBlockBuffer, 0, 1); l_TotalBytesProcessed += BlockSize;
					i_PartialBlockFill = 0; ibStart += i; cbSize -= i;
				}
			}

			// For as long as we have full blocks, process them.
			if(cbSize >= BlockSize) {
				ProcessBlock(array, ibStart, cbSize / BlockSize);
				l_TotalBytesProcessed += cbSize - cbSize % BlockSize;
			}

			// If we still have some bytes left, store them for later.
			int bytesLeft = cbSize % BlockSize;
			if(bytesLeft != 0) {
				Buffer.BlockCopy(array, ((cbSize - bytesLeft) + ibStart), ba_PartialBlockBuffer, 0, bytesLeft);
				i_PartialBlockFill = bytesLeft;
			}
		}


		/// <summary>Performs any final activities required by the hash algorithm.</summary>
		/// <returns>The final hash value.</returns>
		protected override byte[] HashFinal() {
			return ProcessFinalBlock(ba_PartialBlockBuffer, 0, i_PartialBlockFill);
		}


		/// <summary>Process a block of data.</summary>
		/// <param name="inputBuffer">The block of data to process.</param>
		/// <param name="inputOffset">Where to start in the block.</param>
		protected abstract void ProcessBlock(byte[] inputBuffer, int inputOffset, int inputLength);


		/// <summary>Process the last block of data.</summary>
		/// <param name="inputBuffer">The block of data to process.</param>
		/// <param name="inputOffset">Where to start in the block.</param>
		/// <param name="inputCount">How many bytes need to be processed.</param>
		/// <returns>The results of the completed hash calculation.</returns>
		protected abstract byte[] ProcessFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);


		internal static class BitTools {
			public static ushort RotLeft(ushort v, int b) {
				uint i = v; i <<= 16; i |= v;
				b %= 16; i >>= b;
				return (ushort)i;
			}
			public static uint RotLeft(uint v, int b) {
                ulong i = v; i <<= 32; i |= v;
				b %= 32; i >>= (32 - b);
				return (uint)i;
			}

			public static void TypeBlindCopy(byte[] sourceArray, int sourceIndex, uint[] destinationArray, int destinationIndex, int sourceLength) {
				//if(sourceIndex + sourceLength > sourceArray.Length || destinationIndex + (sourceLength + 3) / 4 > destinationArray.Length || sourceLength % 4 != 0) throw new ArgumentException("BitTools.TypeBlindCopy: index or length boundary mismatch.");
				for(int iCtr = 0;iCtr < sourceLength;iCtr += 4, sourceIndex += 4, ++destinationIndex) destinationArray[destinationIndex] = BitConverter.ToUInt32(sourceArray, sourceIndex);
			}
			public static void TypeBlindCopy(uint[] sourceArray, int sourceIndex, byte[] destinationArray, int destinationIndex, int sourceLength) {
				//if(sourceIndex + sourceLength > sourceArray.Length || destinationIndex + sourceLength * 4 > destinationArray.Length) throw new ArgumentException("BitTools.TypeBlindCopy: index or length boundary mismatch.");
				for(int iCtr = 0;iCtr < sourceLength;++iCtr, ++sourceIndex, destinationIndex += 4) Array.Copy(BitConverter.GetBytes(sourceArray[sourceIndex]), 0, destinationArray, destinationIndex, 4);
			}
			public static void TypeBlindCopy(byte[] sourceArray, int sourceIndex, ulong[] destinationArray, int destinationIndex, int sourceLength) {
				//if(sourceIndex + sourceLength > sourceArray.Length || destinationIndex + (sourceLength + 7) / 8 > destinationArray.Length || sourceLength % 8 != 0) throw new ArgumentException("BitTools.TypeBlindCopy: index or length boundary mismatch.");
				for(int iCtr = 0;iCtr < sourceLength;iCtr += 8, sourceIndex += 8, ++destinationIndex) destinationArray[destinationIndex] = BitConverter.ToUInt64(sourceArray, sourceIndex);
			}
			public static void TypeBlindCopy(ulong[] sourceArray, int sourceIndex, byte[] destinationArray, int destinationIndex, int sourceLength) {
				//if(sourceIndex + sourceLength > sourceArray.Length || destinationIndex + sourceLength * 8 > destinationArray.Length) throw new ArgumentException("BitTools.TypeBlindCopy: index or length boundary mismatch.");
				for(int iCtr = 0;iCtr < sourceLength;++iCtr, ++sourceIndex, destinationIndex += 8) Array.Copy(BitConverter.GetBytes(sourceArray[sourceIndex]), 0, destinationArray, destinationIndex, 8);
			}
		}
	}
}
