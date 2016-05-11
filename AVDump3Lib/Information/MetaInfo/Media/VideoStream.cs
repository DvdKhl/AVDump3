using System.Globalization;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public class VideoStream : MediaStream {
		public static readonly MetaInfoItemType IsInterlacedType = new MetaInfoItemType("IsInterlaced", null, typeof(bool), "");
		public static readonly MetaInfoItemType HasAlphaType = new MetaInfoItemType("HasAlpha", null, typeof(bool), "");
		public static readonly MetaInfoItemType StereoModeType = new MetaInfoItemType("StereoMode", null, typeof(StereoModes), "");

		public static readonly MetaInfoItemType PixelDimensionsType = new MetaInfoItemType("PixelDimensions", null, typeof(Dimensions), "");
		public static readonly MetaInfoItemType DisplayDimensionsType = new MetaInfoItemType("DisplayDimensions", null, typeof(Dimensions), "");
		public static readonly MetaInfoItemType DisplayUnitType = new MetaInfoItemType("DisplayUnit", null, typeof(DisplayUnits), "");
		public static readonly MetaInfoItemType AspectRatioBehaviorType = new MetaInfoItemType("AspectRatioBehavior", null, typeof(AspectRatioBehaviors), "");

		public static readonly MetaInfoItemType DisplayAspectRatioType = new MetaInfoItemType("DisplayAspectRatio", null, typeof(double), "");
		public static readonly MetaInfoItemType PixelAspectRatioType = new MetaInfoItemType("PixelAspectRatio", null, typeof(double), "");
		public static readonly MetaInfoItemType StorageAspectRatioType = new MetaInfoItemType("StorageAspectRatio", null, typeof(double), "");

		public static readonly MetaInfoItemType PixelCropType = new MetaInfoItemType("PixelCrop", null, typeof(CropSides), "");
		public static readonly MetaInfoItemType ColorSpaceType = new MetaInfoItemType("ColorSpace", null, typeof(int), "");

        public VideoStream() : base(MediaProvider.VideoStreamType) { }
	}
	public enum StereoModes { Mono, LeftRight, TopBottom, Checkboard, RowInterleaved, ColumnInterleaved, FrameAlternating, Reversed = 1 << 30, Other = 1 << 31, AnaGlyph, CyanRed, GreenMagenta }
	public enum DisplayUnits { Invalid, Pixel, Meter, AspectRatio, Unknown }
	public enum AspectRatioBehaviors { Invalid, FreeResizing, KeepAR, Fixed, Unknown }

	public class Dimensions {
		public Dimensions(int width, int height) {
			Width = width;
			Height = height;
		}
		public int Width { get; private set; }
		public int Height { get; private set; }

		public override string ToString() { return string.Format("{0}, {1}", Width, Height); }
	}
	public class CropSides {
		public CropSides(int top, int right, int bottom, int left) {
			Top = top;
			Left = left;
			Right = right;
			Bottom = bottom;
		}
		public int Top { get; private set; }
		public int Left { get; private set; }
		public int Right { get; private set; }
		public int Bottom { get; private set; }

		public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", Top, Right, Bottom, Left); }
	}
}
