using System;
using System.Collections.Generic;
using CSEBML;
using CSEBML.DocTypes;
using System.Collections;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska {
    public abstract class Section : IEnumerable<KeyValuePair<string, object>> {
		public long? SectionSize { get; protected set; }

		internal void Read(EBMLReader reader, ElementInfo elemInfo) {
			SectionSize = elemInfo.DataLength;

			using(reader.EnterElement(elemInfo)) {
				ElementInfo elementInfo;
				try {
					while((elementInfo = reader.Next()) != null) {
						try {
							if(!ProcessElement(reader, elementInfo) && !IsGlobalElement(elementInfo)) {
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
		}

		internal void ContinueRead(EBMLReader reader, ElementInfo elemInfo) {
			SectionSize = elemInfo.DataLength;

			try {
				do {
					try {
						if(!ProcessElement(reader, elemInfo) && !IsGlobalElement(elemInfo)) {
							//Debug.Print("Unprocessed Item: " + elementInfo.ToDetailedString());
						}
					} catch(Exception) {
						//TODO: Add Issue
					}
				} while((elemInfo = reader.Next()) != null);

				Validate();
			} catch(Exception) {
				//TODO: Add Issue
			}
		}


		internal static bool IsGlobalElement(ElementInfo elementInfo) {
			return elementInfo.DocElement.Id == EBMLDocType.CRC32.Id || elementInfo.DocElement.Id == EBMLDocType.Void.Id;
		}
		internal static void CreateReadAdd<T>(T section, EBMLReader reader, ElementInfo elemInfo, EbmlList<T> lst) where T : Section {
			section.Read(reader, elemInfo);
			lst.Add(section);
		}
		internal static T CreateRead<T>(T section, EBMLReader reader, ElementInfo elemInfo) where T : Section {
			section.Read(reader, elemInfo);
			return section;
		}

		protected abstract bool ProcessElement(EBMLReader reader, ElementInfo elementInfo);
		protected abstract void Validate();

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public abstract IEnumerator<KeyValuePair<string, object>> GetEnumerator();

		protected KeyValuePair<string, object> CreatePair(string key, object value) { return new KeyValuePair<string, object>(key, value); }

	}
}
