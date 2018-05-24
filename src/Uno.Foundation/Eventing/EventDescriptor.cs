using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Diagnostics.Eventing
{
	/// <summary>
	/// Defines an event for the tracing subsystem
	/// </summary>
	public class EventDescriptor
	{
		public EventDescriptor(int eventId, int? task = null, EventOpcode opcode = EventOpcode.Info, byte channel = 0, byte level = 0, long keywords = 0, byte version = 1, long activityId = 0, long relatedActivityId = 0)
		{
			EventId = eventId;
			Version = version;
			Channel = channel;
			Level = level;
			Opcode = opcode;
			Task = task ?? EventId;
			Keywords = keywords;
			ActivityId = activityId;
			RelatedActivityId = relatedActivityId;
		}

		public int EventId { get; }

		public byte Version { get; }

		public byte Channel { get; }

		public byte Level { get; }

		public EventOpcode Opcode { get; }

		public int Task { get; }

		public long Keywords { get; }

		public long ActivityId { get; }

		public long RelatedActivityId { get; }
	}
}
