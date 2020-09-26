using System;

namespace Windows.ApplicationModel.Calls
{
	// without this fields/methods PhoneCallHistoryEntry has no sense :)

	public partial class PhoneCallHistoryEntry
	{
		public bool IsMissed { get; set; }

		public bool IsIncoming { get; set; }

		public TimeSpan? Duration { get; set; }

		public DateTimeOffset StartTime { get; set; }

		public PhoneCallHistoryEntryAddress Address { get; set; }

		public bool IsVoicemail { get; set; }

		public PhoneCallHistoryEntry()
		{
			// constructor defined in UWP: https://docs.microsoft.com/en-us/uwp/api/Windows.ApplicationModel.Calls.PhoneCallHistoryEntry
			// nothing to do, but will be required in future to set SourceIdKind
		}

	}
}
