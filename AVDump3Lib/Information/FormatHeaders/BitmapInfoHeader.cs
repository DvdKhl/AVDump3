namespace AVDump3Lib.Information.FormatHeaders;


public class BitmapInfoHeader {
	public const int LENGTH = 40;

	public uint Size { get; private set; }
	public int Width { get; private set; }
	public int Height { get; private set; }
	public ushort Planes { get; private set; }
	public ushort BitCount { get; private set; }
	public uint Compression { get; private set; }
	public uint SizeImage { get; private set; }
	public int XPelsPerMeter { get; private set; }
	public int YPelsPerMeter { get; private set; }
	public uint ClrUsed { get; private set; }
	public uint ClrImportant { get; private set; }

	public string FourCC => new(new char[] { (char)((Compression >> 00) & 0xFF), (char)((Compression >> 08) & 0xFF), (char)((Compression >> 16) & 0xFF), (char)((Compression >> 24) & 0xFF) });

	public BitmapInfoHeader(byte[] b) {
		if(b == null || b.Length < 40) throw new ArgumentException("Passed array need to be at least 40 bytes long", nameof(b));

		var pos = 0;
		Size = BitConverter.ToUInt32(b, pos); pos += 4;
		Width = BitConverter.ToInt32(b, pos); pos += 4;
		Height = BitConverter.ToInt32(b, pos); pos += 4;
		Planes = BitConverter.ToUInt16(b, pos); pos += 2;
		BitCount = BitConverter.ToUInt16(b, pos); pos += 2;
		Compression = BitConverter.ToUInt32(b, pos); pos += 4;
		SizeImage = BitConverter.ToUInt32(b, pos); pos += 4;
		XPelsPerMeter = BitConverter.ToInt32(b, pos); pos += 4;
		YPelsPerMeter = BitConverter.ToInt32(b, pos); pos += 4;
		ClrUsed = BitConverter.ToUInt32(b, pos); pos += 4;
		ClrImportant = BitConverter.ToUInt32(b, pos);
	}
}
