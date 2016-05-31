using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace AVDump3Lib {
    [Serializable]
    public abstract class AVD3LibException : Exception {
        public DateTimeOffset ThrownOn { get; } = DateTimeOffset.Now;

        public AVD3LibException() { }
        public AVD3LibException(string message) : base(message) { }
        public AVD3LibException(string message, Exception inner) : base(message, inner) { }
        protected AVD3LibException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public XElement ToXElement(bool skipInformationElement, bool includePersonalData) {
            return new XElement(GetType().Name,
                !skipInformationElement ? AddEnvironmentInfo(includePersonalData) : null,
                new XElement("Message", Message),
                ToXElementAdditional(includePersonalData),
                new XElement("Data", Data?.Cast<DictionaryEntry>().Select(
                    x => new XElement(x.Key.ToString(), HandleSensitiveData(x.Value, includePersonalData)))),
                new XElement("Cause", ToXElement(InnerException, includePersonalData)),
                new XElement("Stacktrace", StackTrace?.Split('\n').Select(x => new XElement("Frame", x.Trim()))),
                new XAttribute("thrownOn", ThrownOn.ToString("yyyy-MM-dd HH:mm:ss.ffff"))
            );
        }

        protected static string HandleSensitiveData(object value, bool includePersonalData) {
            if(includePersonalData && value is SensitiveData) {
                return ((SensitiveData)value).Data.ToString();
            } else {
                return value.ToString();
            }
        }

        private static XElement AddEnvironmentInfo(bool includePersonalData) {
            XElement infoElem = new XElement("Information");
            infoElem.Add(new XElement("EntryAssemblyVersion", Assembly.GetEntryAssembly().GetName().Version));
            infoElem.Add(new XElement("LibVersion", Assembly.GetExecutingAssembly().GetName().Version));
            infoElem.Add(new XElement("SessionId", Process.GetCurrentProcess().SessionId));
            infoElem.Add(new XElement("IntPtr.Size", IntPtr.Size));
            infoElem.Add(new XElement("Framework", Environment.Version));
            infoElem.Add(new XElement("OSVersion", Environment.OSVersion.VersionString));
            if(includePersonalData) infoElem.Add(new XElement("Commandline", Environment.CommandLine));
            infoElem.Add(new XElement("Is64BitOperatingSystem", Environment.Is64BitOperatingSystem));
            infoElem.Add(new XElement("Is64BitProcess", Environment.Is64BitProcess));
            infoElem.Add(new XElement("ProcessorCount", Environment.ProcessorCount));
            infoElem.Add(new XElement("UserInteractive", Environment.UserInteractive));
            infoElem.Add(new XElement("WorkingSet", Environment.WorkingSet));

            Type type = Type.GetType("Mono.Runtime");
            if(type != null) {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if(displayName != null) infoElem.Add(new XElement("Mono", displayName.Invoke(null, null)));
            }

            return infoElem;
        }

        protected virtual IEnumerable<XElement> ToXElementAdditional(bool includePersonalData) { yield break; }

        protected static XElement ToXElement(Exception ex, bool includePersonalData) {
            XElement exElem;

            if(ex is AVD3LibException) {
                var avd3LibEx = (AVD3LibException)ex;
                exElem = avd3LibEx.ToXElement(true, includePersonalData);

            } else {
                exElem = new XElement(ex.GetType().Name,
                    new XElement("Message", ex.Message),
                    new XElement("Stacktrace", ex.StackTrace?.Split('\n').Select(x => new XElement("Frame", x.Trim()))),
                    new XElement("Data", ex.Data?.Cast<DictionaryEntry>().Select(
                        x => new XElement(x.Key.ToString(), HandleSensitiveData(x.Value, includePersonalData))))
                );

                if(ex is AggregateException) {
                    var aggEx = (AggregateException)ex;
                    exElem.Add(new XElement("Cause", aggEx.Flatten().InnerExceptions.Select(x => ToXElement(x, includePersonalData))));

                } else if(ex.InnerException != null) {
                    exElem.Add(new XElement("Cause", ToXElement(ex.InnerException, includePersonalData)));
                }
            }

            return exElem;
        }
    }

    [Serializable]
    public class SensitiveData {
        private static readonly int salt = Process.GetCurrentProcess().SessionId;
        private static readonly SHA1 sha1 = SHA1.Create();

        private static string ComputeHash(string value) {
            lock(sha1) { //TODO: Good Enough? We're not protecting banks here.
                return BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(salt + value))).Replace("-", "");
            }
        }

        public object Data { get; private set; }
        public SensitiveData(object data) { Data = data; }
        public override string ToString() { return "Hidden(" + ComputeHash(Data?.ToString()) + ")"; }
    }
}
