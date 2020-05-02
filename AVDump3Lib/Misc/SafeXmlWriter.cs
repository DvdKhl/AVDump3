using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AVDump3Lib.Misc {
	public sealed class StringWriterWithEncoding : StringWriter {
		private readonly Encoding encoding;

		public StringWriterWithEncoding(Encoding encoding) {
			this.encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
		}

		public override Encoding Encoding => encoding;
	}

	public class SafeXmlWriter : XmlTextWriter {
		private readonly bool lowerCaseElements = false;

		public SafeXmlWriter(TextWriter tw) : base(tw) { Formatting = Formatting.Indented; }
		public SafeXmlWriter(string filename, Encoding encoding) : base(filename, encoding) { Formatting = Formatting.Indented; }
		public SafeXmlWriter(Stream stream, Encoding encoding) : base(stream, encoding) { Formatting = Formatting.Indented; }



		private readonly StringBuilder sb = new StringBuilder();
		public override void WriteString(string text) {
			foreach(var character in text) if(IsLegalXmlChar(character)) sb.Append(character);
			base.WriteString(sb.ToString());
			sb.Length = 0;
		}

		public override void WriteStartElement(string prefix, string localName, string ns) {
			base.WriteStartElement(prefix, lowerCaseElements ? localName.ToLowerInvariant() : localName, ns);
		}

		public static bool IsLegalXmlChar(int character) => character == 0x9 || character == 0xA || character == 0xD || character >= 0x20 && character <= 0xD7FF || character >= 0xE000 && character <= 0xFFFD || character >= 0x10000 && character <= 0x10FFFF;
	}
}
