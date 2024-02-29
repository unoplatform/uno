namespace Windows.ApplicationModel.Calls;

/// <summary>
/// Address book information for a contact used by PhoneCallHistoryEntry objects.
/// </summary>
public partial class PhoneCallHistoryEntryAddress
{
	/// <summary>
	/// Creates a new empty PhoneCallHistoryEntryAddress object.
	/// </summary>
	public PhoneCallHistoryEntryAddress()
	{
		RawAddressKind = PhoneCallHistoryEntryRawAddressKind.Custom;
	}

	/// <summary>
	/// Creates a new PhoneCallHistoryEntryAddress object with an initial address.
	/// </summary>
	/// <param name="rawAddress">The address to initiailize to the RawAddress property.</param>
	/// <param name="rawAddressKind">The type of address represented by rawAddress.</param>
	public PhoneCallHistoryEntryAddress(string rawAddress, PhoneCallHistoryEntryRawAddressKind rawAddressKind)
	{
		RawAddressKind = rawAddressKind;
		RawAddress = rawAddress;
	}

	/// <summary>
	/// Gets or sets the type of address indicated by RawAddress.
	/// </summary>
	public PhoneCallHistoryEntryRawAddressKind RawAddressKind { get; set; }

	/// <summary>
	/// Gets or sets the address information for this contact.
	/// </summary>
	public string? RawAddress { get; set; } = "";

	/// <summary>
	/// Get or sets the display name for this entry.
	/// </summary>
	public string? DisplayName { get; set; } = "";
}
