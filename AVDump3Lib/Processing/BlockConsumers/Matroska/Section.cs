using BXmlLib;
using BXmlLib.DataSource;
using BXmlLib.DocType;
using BXmlLib.DocTypes.Ebml;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska {
	public abstract class Section : IEnumerable<KeyValuePair<string, object>> {
		public long? SectionSize { get; protected set; }

		internal void Read(IBXmlReader reader) {
			SectionSize = reader.Header.DataLength != ~BXmlElementHeader.UnknownLength ? reader.Header.DataLength : default(long?);

			using(reader.EnterElement()) {
				try {
					while(reader.Next()) {
						try {
							if(!ProcessElement(reader) && !IsGlobalElement(reader.DocElement)) {
								//Debug.Print("Unprocessed Item: " + elementInfo.ToDetailedString());
							}
						} catch(Exception) {
							//TODO: Add Issue
						}
					}
					Validate();
				} catch(Exception) {
					//TODO: Add Issue
				}
			}

			if(reader.Header.DataLength == ~BXmlElementHeader.UnknownLength) {

			}
		}

		internal void ContinueRead(IBXmlReader reader) {
			SectionSize = reader.Header.DataLength != ~BXmlElementHeader.UnknownLength ? reader.Header.DataLength : default(long?);

			try {
				do {
					try {
						if(!ProcessElement(reader) && !IsGlobalElement(reader.DocElement)) {
							//Debug.Print("Unprocessed Item: " + elementInfo.ToDetailedString());
						}
					} catch(Exception) {
						//TODO: Add Issue
					}
				} while(reader.Next());

				Validate();
			} catch(Exception) {
				//TODO: Add Issue
			}
		}


		internal static bool IsGlobalElement(BXmlDocElement docElement) {
			return docElement == EbmlDocType.CRC32 || docElement == EbmlDocType.Void;
		}
		internal static void CreateReadAdd<T>(T section, IBXmlReader reader, EbmlList<T> lst) where T : Section {
			section.Read(reader);
			lst.Add(section);
		}
		internal static T CreateRead<T>(T section, IBXmlReader reader) where T : Section {
			section.Read(reader);
			return section;
		}

		protected abstract bool ProcessElement(IBXmlReader reader);
		protected abstract void Validate();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public abstract IEnumerator<KeyValuePair<string, object>> GetEnumerator();

		protected KeyValuePair<string, object> CreatePair(string key, object? value) => new KeyValuePair<string, object?>(key, value);

	}
}
