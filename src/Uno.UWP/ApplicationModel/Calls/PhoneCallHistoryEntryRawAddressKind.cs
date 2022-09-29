#nullable disable

namespace Windows.ApplicationModel.Calls;

/// <summary>
/// The type of address used by the PhoneCallHistoryEntryAddress.
/// </summary>
public enum PhoneCallHistoryEntryRawAddressKind
{
	/// <summary>
	/// The raw address is a phone number.
	/// </summary>
	PhoneNumber,

	/// <summary>
	/// The raw address is a custom string.
	/// </summary>
	Custom,
}
