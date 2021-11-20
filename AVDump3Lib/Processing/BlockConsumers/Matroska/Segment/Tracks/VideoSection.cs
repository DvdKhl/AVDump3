using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks;

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
	public byte[] ColorSpace => colorSpace != null ? (byte[])colorSpace.Clone() : null;
	public ulong PixelWidth { get; private set; }
	public ulong PixelHeight { get; private set; }
	public ARType AspectRatioType => aspectRatioType ?? ARType.FreeResizing;  //Default: FreeResizing (0)
	public OldStereoModes? OldStereoMode { get; private set; }
	public ulong AlphaMode => alphaMode ?? 0;  //Default: FreeResizing (0)
	public StereoModes StereoMode => stereoMode ?? StereoModes.Mono;  //Default: FreeResizing (0)
	public bool Interlaced => interlaced ?? false;  //Default: FreeResizing (0)
	public ulong PixelCropBottom => pixelCropBottom ?? 0;  //Default: 0
	public ulong PixelCropTop => pixelCropTop ?? 0;  //Default: 0
	public ulong PixelCropLeft => pixelCropLeft ?? 0;  //Default: 0
	public ulong PixelCropRight => pixelCropRight ?? 0;  //Default: 0
	public ulong DisplayWidth => displayWidth ?? PixelWidth;  //Default: $PixelWidth
	public ulong DisplayHeight => displayHeight ?? PixelHeight;  //Default: $PixelHeight
	public Unit DisplayUnit => displayUnit ?? Unit.Pixels;  //Default: Pixels (0)

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.PixelWidth) {
			PixelWidth = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.PixelHeight) {
			PixelHeight = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.PixelCropBottom) {
			pixelCropBottom = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.PixelCropTop) {
			pixelCropTop = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.PixelCropLeft) {
			pixelCropLeft = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.PixelCropRight) {
			pixelCropRight = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.DisplayWidth) {
			displayWidth = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.DisplayHeight) {
			displayHeight = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.DisplayUnit) {
			displayUnit = (Unit)(ulong)reader.RetrieveValue();

		} else if(reader.DocElement == MatroskaDocType.AspectRatioType) {
			aspectRatioType = (ARType)(ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.OldStereoMode) {
			OldStereoMode = (OldStereoModes)(ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.StereoMode) {
			stereoMode = (StereoModes)(ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.AlphaMode) {
			alphaMode = (ulong)reader.RetrieveValue();

		} else if(reader.DocElement == MatroskaDocType.FrameRate) {
			FrameRate = (double)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.FlagInterlaced) {
			interlaced = (ulong)reader.RetrieveValue() == 1;
		} else if(reader.DocElement == MatroskaDocType.ColourSpace) {
			colorSpace = (byte[])reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.GammaValue) {
			Gamma = (double)reader.RetrieveValue();
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
