namespace Windows.ApplicationModel.Calls;

/// <summary>
/// Represents a collection of information about the phone lines available on a device.
/// </summary>
public partial class PhoneCallHistoryStore
{
	internal PhoneCallHistoryStore()
	{		
	}

	/// <summary>
	/// Retrieves a default phone call history entry that reads all entries.
	/// </summary>
	/// <returns>A reader that can be used to go through the phone call log entries.</returns>
	public PhoneCallHistoryEntryReader GetEntryReader() => new PhoneCallHistoryEntryReader();
}
