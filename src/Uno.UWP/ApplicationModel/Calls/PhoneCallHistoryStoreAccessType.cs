namespace Windows.ApplicationModel.Calls
{
	public enum PhoneCallHistoryStoreAccessType
	{
		// option without support - limited to app
		[global::Uno.NotImplemented]
		AppEntriesReadWrite,

		AllEntriesLimitedReadWrite = 1,
		AllEntriesReadWrite = 2
	}
}
