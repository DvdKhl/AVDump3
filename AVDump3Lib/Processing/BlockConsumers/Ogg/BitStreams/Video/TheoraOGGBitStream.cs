using System;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
    public class TheoraOGGBitStream : VideoOGGBitStream {
		public override string CodecName { get { return "Theora"; } }
		public override string CodecVersion { get; protected set; }

		public TheoraOGGBitStream(byte[] header, int offset) :base(true) {
			CodecVersion = header[offset + 7] + "." + header[offset + 8] + "." + header[offset + 9];
			Width = header[offset + 14] << 16 | header[offset + 15] << 8 | header[offset + 16];
			Height = header[offset + 17] << 16 | header[offset + 18] << 8 | header[offset + 19];
			FrameRate = (header[offset + 22] << 24 | header[offset + 23] << 16 | header[offset + 24] << 8 | header[offset + 25]) / (double)(header[offset + 26] << 24 | header[offset + 27] << 16 | header[offset + 28] << 8 | header[offset + 29]);
			//PAR = (header[30] << 16 | header[31] << 8 | header[32]) / (double)(header[33] << 16 | header[34] << 8 | header[35]);
		}


        public override void ProcessPage(OggPage page) {
			base.ProcessPage(page);

			var frameIndex = BitConverter.ToInt64(page.GranulePosition, 0);
			if(FrameCount < (int)frameIndex) FrameCount = (int)frameIndex;
		}

		//[StructLayout(LayoutKind.Sequential, Size = 35, Pack = 1)]
		//public struct Theora {
		//    public byte VMAJ;
		//    public byte VMIN;
		//    public byte VREV;
		//    public short FMBW;
		//    public short FMBH;
		//    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		//    public byte[] picw;
		//    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		//    public byte[] pich;
		//    public byte PICX;
		//    public byte PICY;
		//    public int FRN;
		//    public int FRD;
		//    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		//    public byte[] parn;
		//    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		//    public byte[] pard;
		//    public byte CS;
		//    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		//    public byte[] NOMBR;
		//    public short otherFields;
		//    public double PAR { get { return (parn[0] << 16 | parn[1] << 8 | parn[2]) / (double)(pard[0] << 16 | pard[1] << 8 | pard[2]); } }
		//    public double FPS { get { return FRN / (double)FRD; } }
		//    public int Width { get { return picw[0] << 16 | picw[1] << 8 | picw[2]; } }
		//    public int Height { get { return pich[0] << 16 | pich[1] << 8 | pich[2]; } }
		//}
	}
}
