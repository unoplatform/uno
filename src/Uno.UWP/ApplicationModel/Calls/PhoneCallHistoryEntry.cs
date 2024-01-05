using System;

namespace Windows.ApplicationModel.Calls;

/// <summary>
/// A collection of information about a phone call for the call history.
/// </summary>
public partial class PhoneCallHistoryEntry
{
	/// <summary>
	/// Creates a new PhoneCallHistoryEntry object.
	/// </summary>
	public PhoneCallHistoryEntry()
	{
		// constructor defined in UWP: https://docs.microsoft.com/en-us/uwp/api/Windows.ApplicationModel.Calls.PhoneCallHistoryEntry
		// nothing to do, but will be required in future to set SourceIdKind
	}

	/// <summary>
	/// Gets or sets whether a phone call was missed.
	/// </summary>
	public bool IsMissed { get; set; }

	/// <summary>
	/// Gets or sets whether a call is an incoming call.
	/// </summary>
	public bool IsIncoming { get; set; }

	/// <summary>
	/// Gets or sets the duration of the call.
	/// </summary>
	public TimeSpan? Duration { get; set; }

	/// <summary>
	/// Gets or sets the start time for this history entry.
	/// </summary>
	public DateTimeOffset StartTime { get; set; }

	/// <summary>
	/// Gets or sets the address book information for this phone call.
	/// </summary>
	public PhoneCallHistoryEntryAddress? Address { get; set; }

	/// <summary>
	/// Gets or sets whether the phone call entry is a voicemail message.
	/// </summary>
	public bool IsVoicemail { get; set; }
}
