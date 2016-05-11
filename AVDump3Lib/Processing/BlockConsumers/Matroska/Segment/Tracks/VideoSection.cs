using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Tracks {
    public class VideoSection : Section {
		private ulong? pixelCropBottom;
		private ulong? pixelCropTop;
		private ulong? pixelCropLeft;
		private ulong? pixelCropRight;
		private ulong? displayWidth;
		private ulong? displayHeight;
		private bool? interlaced;
		private byte[] colorSpace;
		private ARType? aspectRatioType;
		private StereoModes? stereoMode;
		private Unit? displayUnit;
		private ulong? alphaMode;

		public double? FrameRate { get; private set; }
		public double? Gamma { get; private set; }
		public byte[] ColorSpace { get { return colorSpace != null ? (byte[])colorSpace.Clone() : null; } }
		public ulong PixelWidth { get; private set; }
		public ulong PixelHeight { get; private set; }
		public ARType AspectRatioType { get { return aspectRatioType ?? ARType.FreeResizing; } } //Default: FreeResizing (0)
		public OldStereoModes? OldStereoMode { get; private set; }
		public ulong AlphaMode { get { return alphaMode ?? 0; } } //Default: FreeResizing (0)
		public StereoModes StereoMode { get { return stereoMode ?? StereoModes.Mono; } } //Default: FreeResizing (0)
		public bool Interlaced { get { return interlaced ?? false; } } //Default: FreeResizing (0)
		public ulong PixelCropBottom { get { return pixelCropBottom ?? 0; } } //Default: 0
		public ulong PixelCropTop { get { return pixelCropTop ?? 0; } } //Default: 0
		public ulong PixelCropLeft { get { return pixelCropLeft ?? 0; } } //Default: 0
		public ulong PixelCropRight { get { return pixelCropRight ?? 0; } } //Default: 0
		public ulong DisplayWidth { get { return displayWidth ?? PixelWidth; } } //Default: $PixelWidth
		public ulong DisplayHeight { get { return displayHeight ?? PixelHeight; } } //Default: $PixelHeight
		public Unit DisplayUnit { get { return displayUnit ?? Unit.Pixels; } } //Default: Pixels (0)

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.PixelWidth.Id) {
				PixelWidth = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.PixelHeight.Id) {
				PixelHeight = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.PixelCropBottom.Id) {
				pixelCropBottom = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.PixelCropTop.Id) {
				pixelCropTop = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.PixelCropLeft.Id) {
				pixelCropLeft = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.PixelCropRight.Id) {
				pixelCropRight = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.DisplayWidth.Id) {
				displayWidth = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.DisplayHeight.Id) {
				displayHeight = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.DisplayUnit.Id) {
				displayUnit = (Unit)(ulong)reader.RetrieveValue(elemInfo);

			} else if(elemInfo.DocElement.Id == MatroskaDocType.AspectRatioType.Id) {
				aspectRatioType = (ARType)(ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.OldStereoMode.Id) {
				OldStereoMode = (OldStereoModes)(ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.StereoMode.Id) {
				stereoMode = (StereoModes)(ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.AlphaMode.Id) {
				alphaMode = (ulong)reader.RetrieveValue(elemInfo);

			} else if(elemInfo.DocElement.Id == MatroskaDocType.FrameRate.Id) {
				FrameRate = (double)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.FlagInterlaced.Id) {
				interlaced = (ulong)reader.RetrieveValue(elemInfo) == 1;
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ColourSpace.Id) {
				colorSpace = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.GammaValue.Id) {
				Gamma = (double)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("FrameRate", FrameRate);
			yield return CreatePair("ColorSpace", ColorSpace);
			yield return CreatePair("PixelWidth", PixelWidth);
			yield return CreatePair("PixelHeight", PixelHeight);
			yield return CreatePair("AspectRatioType", AspectRatioType);
			yield return CreatePair("StereoMode", StereoMode);
			yield return CreatePair("AlphaMode", AlphaMode);
			yield return CreatePair("OldStereoMode", OldStereoMode);
			yield return CreatePair("Interlaced", Interlaced);
			yield return CreatePair("PixelCropBottom", PixelCropBottom);
			yield return CreatePair("PixelCropTop", PixelCropTop);
			yield return CreatePair("PixelCropLeft", PixelCropLeft);
			yield return CreatePair("PixelCropRight", PixelCropRight);
			yield return CreatePair("DisplayWidth", DisplayWidth);
			yield return CreatePair("DisplayHeight", DisplayHeight);
			yield return CreatePair("DisplayUnit", DisplayUnit);
		}


		public enum Unit { Pixels, Centimeters, Inches, AspectRatio }
		public enum ARType { FreeResizing, KeepAR, Fixed }
		public enum StereoModes { Mono = 0, LeftRight = 1, BottomTop = 2, TopBottom = 3, CheckBoardRight = 4, CheckboardLeft = 5, RowInterleavedRight = 6, RowInterleavedLeft = 7, ColumnInterleavedRight = 8, ColumnInterleavedLeft = 9, AnaGlyphCyanRed = 10, RightLeft = 11, AnaGlyphGreenMagenta = 12, AlternatingFramesRight = 13, AlternatingFramesLeft = 14 }
		public enum OldStereoModes { Mono = 0, RightEye = 1, LeftEye = 2, Both = 3 }

	}
}
