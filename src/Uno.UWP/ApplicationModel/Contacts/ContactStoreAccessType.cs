namespace Windows.ApplicationModel.Contacts
{
	public enum ContactStoreAccessType
	{
		AllContactsReadOnly = 1,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented]
		AppContactsReadWrite = 0,
		#endif

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented]
		AllContactsReadWrite = 2,
		#endif
	}
}
