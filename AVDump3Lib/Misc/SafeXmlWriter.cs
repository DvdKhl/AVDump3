using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AVDump3Lib.Misc {
	public class SafeXmlWriter : XmlTextWriter {
		bool lowerCaseElements;

		public SafeXmlWriter(TextWriter tw, Formatting formatting = Formatting.Indented, bool lowerCaseElements = false) : base(tw) { Formatting = formatting; this.lowerCaseElements = lowerCaseElements; }
		public SafeXmlWriter(Stream stream, Encoding encoding, Formatting formatting = Formatting.Indented, bool lowerCaseElements = false) : base(stream, encoding) { Formatting = formatting; this.lowerCaseElements = lowerCaseElements; }
		public SafeXmlWriter(string filename, Encoding encoding, Formatting formatting = Formatting.Indented, bool lowerCaseElements = false) : base(filename, encoding) { Formatting = formatting; this.lowerCaseElements = lowerCaseElements; }



		private StringBuilder sb = new StringBuilder();
		public override void WriteString(string text) {
			foreach(var character in text) if(IsLegalXmlChar(character)) sb.Append(character);
			base.WriteString(sb.ToString());
			sb.Length = 0;
		}

		public override void WriteStartElement(string prefix, string localName, string ns) {
			base.WriteStartElement(prefix, lowerCaseElements ? localName.ToLower() : localName, ns);
		}

		public static bool IsLegalXmlChar(int character) { return (character == 0x9 || character == 0xA || character == 0xD || (character >= 0x20 && character <= 0xD7FF) || (character >= 0xE000 && character <= 0xFFFD) || (character >= 0x10000 && character <= 0x10FFFF)); }
	}
}
