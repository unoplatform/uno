#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IContactInstantMessageFieldFactory 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactInstantMessageField CreateInstantMessage( string userName);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactInstantMessageField CreateInstantMessage( string userName,  global::Windows.ApplicationModel.Contacts.ContactFieldCategory category);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactInstantMessageField CreateInstantMessage( string userName,  global::Windows.ApplicationModel.Contacts.ContactFieldCategory category,  string service,  string displayText,  global::System.Uri verb);
		#endif
	}
}
