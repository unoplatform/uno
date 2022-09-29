#nullable disable

using Uno;

namespace Windows.ApplicationModel.Calls;

/// <summary>
/// The type of store you want to retrieve.
/// </summary>
public enum PhoneCallHistoryStoreAccessType
{
	/// <summary>
	/// Only entries created by this application should have read and write permissions.
	/// </summary>
	[NotImplemented]
	AppEntriesReadWrite,

	/// <summary>
	/// All of the entries should have limited read and write permissions.
	/// </summary>
	AllEntriesLimitedReadWrite = 1,

	/// <summary>
	/// All the entries should have full read and write permissions.
	/// </summary>
	AllEntriesReadWrite = 2
}
