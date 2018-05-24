using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Diagnostics.Eventing
{
	/// <summary>
	/// Identifies a tracing event activity.
	/// </summary>
    public class EventActivity
    {
		public EventActivity(long activityId)
		{
			Id = activityId;
		}

		public long Id { get; }
	}
}
