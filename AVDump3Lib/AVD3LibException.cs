using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace AVDump3Lib {
	public abstract class AVD3LibException : Exception {
		public DateTimeOffset ThrownOn { get; } = DateTimeOffset.Now;

		public AVD3LibException() { }
		public AVD3LibException(string message) : base(message) { }
		public AVD3LibException(string message, Exception inner) : base(message, inner) { }

		public XElement ToXElement(bool skipInformationElement, bool includePersonalData) {
			return new XElement(GetType().Name,
				!skipInformationElement ? AddEnvironmentInfo() : null,
				new XElement("Message", Message),
				ToXElementAdditional(includePersonalData),
				new XElement("Data", Data?.Cast<DictionaryEntry>().Select(x => new XElement(x.Key.ToString(), HandleSensitiveData(x.Value, includePersonalData)))),
				new XElement("Cause", ToXElement(InnerException, includePersonalData)),
				new XElement("Stacktrace", StackTrace?.Split('\n').Select(x => new XElement("Frame", x.Trim()))),
				new XAttribute("thrownOn", ThrownOn.ToString("yyyy-MM-dd HH:mm:ss.ffff", CultureInfo.InvariantCulture))
			);
		}

		protected static string HandleSensitiveData(object? value, bool includePersonalData) {
			if(includePersonalData && value is SensitiveData) {
				return ((SensitiveData)value).Data.ToString() ?? "";
			} else {
				return value?.ToString() ?? "";
			}
		}

		private static XElement AddEnvironmentInfo() {
			var infoElem = new XElement("Information");
			infoElem.Add(new XElement("EntryAssemblyVersion", Assembly.GetEntryAssembly()?.GetName().Version));
			infoElem.Add(new XElement("LibVersion", Assembly.GetExecutingAssembly().GetName().Version));
			infoElem.Add(SensitiveData.GetSessionElement);
			infoElem.Add(new XElement("Framework", Environment.Version));
			infoElem.Add(new XElement("OSVersion", Environment.OSVersion.VersionString));
			infoElem.Add(new XElement("IntPtr.Size", IntPtr.Size));
			infoElem.Add(new XElement("Is64BitOperatingSystem", Environment.Is64BitOperatingSystem));
			infoElem.Add(new XElement("Is64BitProcess", Environment.Is64BitProcess));
			infoElem.Add(new XElement("ProcessorCount", Environment.ProcessorCount));
			infoElem.Add(new XElement("UserInteractive", Environment.UserInteractive));
			infoElem.Add(new XElement("SystemPageSize", Environment.SystemPageSize));
			infoElem.Add(new XElement("WorkingSet", Environment.WorkingSet));

			return infoElem;
		}

		protected virtual IEnumerable<XElement> ToXElementAdditional(bool includePersonalData) { yield break; }


		protected static XElement? ToXElement(Exception? ex, bool includePersonalData) {
			if(ex is null) return null;

			XElement exElem;
			if(ex is AVD3LibException avd3LibEx) {
				exElem = avd3LibEx.ToXElement(true, includePersonalData);

			} else {
				exElem = new XElement(ex.GetType().Name,
					new XElement("Message", ex.Message),
					new XElement("Stacktrace", ex.StackTrace?.Split('\n').Select(x => new XElement("Frame", x.Trim()))),
					new XElement("Data", ex.Data?.Cast<DictionaryEntry>().Select(
						x => new XElement(x.Key.ToString(), HandleSensitiveData(x.Value, includePersonalData))
					))
				);

				if(ex is AggregateException aggEx) {
					exElem.Add(new XElement("Cause", aggEx.Flatten().InnerExceptions.Select(x => ToXElement(x, includePersonalData))));

				} else if(ex.InnerException != null) {
					exElem.Add(new XElement("Cause", ToXElement(ex.InnerException, includePersonalData)));
				}
			}

			return exElem;
		}
	}


	public class AVD3ForceMajeureException : AVD3LibException {
		public string RemedyActionMessage { get; set; } = "";

		public AVD3ForceMajeureException() { }

		public AVD3ForceMajeureException(string message) : base(message) { }
		public AVD3ForceMajeureException(string message, Exception innerException) : base(message, innerException) { }

		public AVD3ForceMajeureException(string remedyActionMessage, string message) : base(message) {
			RemedyActionMessage = remedyActionMessage;
		}
		public AVD3ForceMajeureException(string remedyActionMessage, string message, Exception innerException) : base(message, innerException) {
			RemedyActionMessage = remedyActionMessage;
		}
	}

	[Serializable]
	public class SensitiveData {
		private static readonly Guid session = Guid.NewGuid();
		private static readonly SHA512 hashObj = SHA512.Create();

		public static XElement GetSessionElement => new XElement("Session", session);

		private static string ComputeHash(string value) {
			lock(hashObj) { //TODO: Good Enough? We're not protecting banks here.
				return BitConverter.ToString(
					hashObj.ComputeHash(Encoding.UTF8.GetBytes(session.ToString() + value))
				).Replace("-", "", StringComparison.InvariantCultureIgnoreCase);
			}
		}

		public object Data { get; private set; }
		public SensitiveData(object data) { Data = data; }
		public override string ToString() { return "Hidden(" + ComputeHash(Data?.ToString() ?? "") + ")"; }
	}
}
