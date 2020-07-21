#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IContactFieldFactory 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactField CreateField( string value,  global::Windows.ApplicationModel.Contacts.ContactFieldType type);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactField CreateField( string value,  global::Windows.ApplicationModel.Contacts.ContactFieldType type,  global::Windows.ApplicationModel.Contacts.ContactFieldCategory category);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactField CreateField( string name,  string value,  global::Windows.ApplicationModel.Contacts.ContactFieldType type,  global::Windows.ApplicationModel.Contacts.ContactFieldCategory category);
		#endif
	}
}
