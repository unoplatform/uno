namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallHistoryEntryAddress
	{
		// without this fields/methods PhoneCallHistoryEntryAddress has no sense :)

		public PhoneCallHistoryEntryRawAddressKind RawAddressKind { get; set; }

		public string RawAddress { get; set; }
		public string DisplayName { get; set; }

		public PhoneCallHistoryEntryAddress(string rawAddress, PhoneCallHistoryEntryRawAddressKind rawAddressKind)
		{
			RawAddressKind = rawAddressKind;
			RawAddress = rawAddress;
		}
		public PhoneCallHistoryEntryAddress()
		{
			RawAddressKind = PhoneCallHistoryEntryRawAddressKind.Custom;
		}

	}
}
