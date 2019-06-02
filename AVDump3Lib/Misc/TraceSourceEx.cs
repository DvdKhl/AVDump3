using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AVDump3Lib.Misc {
	public class TraceSourceEx {
		public TraceSource Base { get; private set; }

		public void OnTrace(Action<TraceSource> onTrace) { if(Base != null) onTrace(Base); }

		public TraceSourceEx(string name, SourceLevels defaultLevel) {
#if TRACE
			Base = new TraceSource(name, defaultLevel);
#endif
		}
		public TraceSourceEx(string name) : this(name, SourceLevels.All) { }


		public IDisposable CreateActivity(string activityName) { return CreateActivity(activityName, false, Guid.NewGuid()); }
		public IDisposable CreateActivity(string activityName, bool skipTransfer) { return CreateActivity(activityName, skipTransfer, Guid.NewGuid()); }

		public IDisposable CreateActivity(string activityName, object obj) { return CreateActivity(activityName, false, obj); }
		public IDisposable CreateActivity(string activityName, Guid guid) { return CreateActivity(activityName, false, guid); }


		public IDisposable CreateActivity(string activityName, bool skipTransfer, Guid guid) {
			if(Base == null) return nopDisposable;
			return new ActivityContext(Base, Trace.CorrelationManager.ActivityId, activityName, guid, skipTransfer);
		}
		public IDisposable CreateActivity(string activityName, bool skipTransfer, object obj) {
			if(Base == null) return nopDisposable;
			var activityId = new Guid(new byte[8].Concat(BitConverter.GetBytes(obj.GetType().GetHashCode())).Concat(BitConverter.GetBytes(obj.GetHashCode())).ToArray());
			return new ActivityContext(Base, Trace.CorrelationManager.ActivityId, activityName, activityId, skipTransfer);
		}



		//public StringDictionary Attributes { get { return Base != null ? Attributes : null; } }
		//public TraceListenerCollection Listeners { get { return Base != null ? Listeners : null; } }
		//public string Name { get { return Base != null ? Name : null; } }
		//public SourceSwitch Switch { get { return Base != null ? Switch : null; } set { } }

		[Conditional("TRACE")]
		public void Close() { if(Base != null) Base.Close(); }
		[Conditional("TRACE")]
		public void Flush() { if(Base != null) Base.Flush(); }
		//protected internal virtual string[] GetSupportedAttributes() { return Base != null ? Base.GetSupportedAttributes() : null; }
		[Conditional("TRACE")]
		public void TraceData(TraceEventType eventType, int id, object data) { if(Base != null) Base.TraceData(eventType, id, data); }
		[Conditional("TRACE")]
		public void TraceData(TraceEventType eventType, int id, params object[] data) { if(Base != null) Base.TraceData(eventType, id, data); }

		[Conditional("TRACE")]
		public void TraceEvent(TraceEventType eventType, int id) { if(Base != null) Base.TraceEvent(eventType, id); }
		[Conditional("TRACE")]
		public void TraceEvent(TraceEventType eventType, int id, string message) { if(Base != null) Base.TraceEvent(eventType, id, message); }
		[Conditional("TRACE")]
		public void TraceEvent(TraceEventType eventType, int id, string format, params object[] args) { if(Base != null) Base.TraceEvent(eventType, id, format, args); }

		[Conditional("TRACE")]
		public void TraceWarning(string message) { if(Base != null) Base.TraceEvent(TraceEventType.Warning, 0, message); }
		[Conditional("TRACE")]
		public void TraceWarning(string format, params object[] args) { if(Base != null) Base.TraceEvent(TraceEventType.Warning, 0, format, args); }

		[Conditional("TRACE")]
		public void TraceError(string message) { if(Base != null) Base.TraceEvent(TraceEventType.Error, 0, message); }
		[Conditional("TRACE")]
		public void TraceError(string format, params object[] args) { if(Base != null) Base.TraceEvent(TraceEventType.Error, 0, format, args); }

		[Conditional("TRACE")]
		public void TraceException(Exception exception, string message) { if(Base != null) Base.TraceData(TraceEventType.Error, 0, message, exception); }
		[Conditional("TRACE")]
		public void TraceException(Exception exception, string format, params object[] args) { if(Base != null) Base.TraceData(TraceEventType.Error, 0, string.Format(format, args), exception); }

		[Conditional("TRACE")]
		public void TraceAndThrow(Exception exception, string message) { TraceException(exception, message); throw exception; }
		[Conditional("TRACE")]
		public void TraceAndThrow(Exception exception, string format, params object[] args) { TraceException(exception, format, args); throw exception; }


		[Conditional("TRACE")]
		public void TraceInformation(string message) { if(Base != null) Base.TraceInformation(message); }
		[Conditional("TRACE")]
		public void TraceInformation(string format, params object[] args) { if(Base != null) Base.TraceInformation(format, args); }
		//[Conditional("TRACE")]
		//public void TraceInformation(string format, IEnumerable<string> args) { if(Base != null) Base.TraceInformation(format.Replace("{0}", string.Join(", ", args))); }
		[Conditional("TRACE")]
		public void TraceInformation(string format, string[] args) { if(Base != null) Base.TraceInformation(format.Replace("{0}", string.Join(", ", args))); }

		[Conditional("TRACE")]
		public void TraceTransfer(int id, string message, Guid relatedActivityId) { if(Base != null) Base.TraceTransfer(id, message, relatedActivityId); }



		private static NOPDisposable nopDisposable = new NOPDisposable();
		private class NOPDisposable : IDisposable { public void Dispose() { } }

		private class ActivityContext : IDisposable {
			private TraceSource ts;
			private string activityName;
			private bool skipTransfer;

			public Guid parentActivityId;


			public ActivityContext(TraceSource ts, Guid parentActivityId, string activityName, Guid newActivityId, bool skipTransfer) {
				this.ts = ts;
				this.activityName = activityName;
				this.parentActivityId = parentActivityId;
				this.skipTransfer = skipTransfer;

				Trace.CorrelationManager.StartLogicalOperation(activityName);

				if(!skipTransfer) ts.TraceTransfer(0, "Transfering to " + activityName, newActivityId);
				Trace.CorrelationManager.ActivityId = newActivityId;

				ts.TraceEvent(TraceEventType.Start, 0, activityName + " started");
			}

			public void Dispose() {
				ts.TraceEvent(TraceEventType.Stop, 0, activityName + " stopped");

				if(!skipTransfer) ts.TraceTransfer(0, "Transfering from " + activityName, parentActivityId);
				Trace.CorrelationManager.ActivityId = parentActivityId;

				Trace.CorrelationManager.StopLogicalOperation();
			}
		}
	}
}
