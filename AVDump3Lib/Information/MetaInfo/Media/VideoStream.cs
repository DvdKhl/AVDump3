using System.Globalization;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public class VideoStream : MediaStream {
		public static readonly MetaInfoItemType<bool> IsInterlacedType = new MetaInfoItemType<bool>("IsInterlaced", null);
		public static readonly MetaInfoItemType<bool> HasAlphaType = new MetaInfoItemType<bool>("HasAlpha", null);
		public static readonly MetaInfoItemType<StereoModes> StereoModeType = new MetaInfoItemType<StereoModes>("StereoMode", null);

		public static readonly MetaInfoItemType<Dimensions> PixelDimensionsType = new MetaInfoItemType<Dimensions>("PixelDimensions", null);
		public static readonly MetaInfoItemType<Dimensions> DisplayDimensionsType = new MetaInfoItemType<Dimensions>("DisplayDimensions", null);
		public static readonly MetaInfoItemType<DisplayUnits> DisplayUnitType = new MetaInfoItemType<DisplayUnits>("DisplayUnit", null);
		public static readonly MetaInfoItemType<AspectRatioBehaviors> AspectRatioBehaviorType = new MetaInfoItemType<AspectRatioBehaviors>("AspectRatioBehavior", null);

		public static readonly MetaInfoItemType<double> DisplayAspectRatioType = new MetaInfoItemType<double>("DisplayAspectRatio", null);
		public static readonly MetaInfoItemType<double> PixelAspectRatioType = new MetaInfoItemType<double>("PixelAspectRatio", null);
		public static readonly MetaInfoItemType<double> StorageAspectRatioType = new MetaInfoItemType<double>("StorageAspectRatio", null);

		public static readonly MetaInfoItemType<CropSides> PixelCropType = new MetaInfoItemType<CropSides>("PixelCrop", null);
		public static readonly MetaInfoItemType<int> ColorSpaceType = new MetaInfoItemType<int>("ColorSpace", null);

        public VideoStream()  { }
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
