#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IContactLocationFieldFactory 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactLocationField CreateLocation( string unstructuredAddress);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactLocationField CreateLocation( string unstructuredAddress,  global::Windows.ApplicationModel.Contacts.ContactFieldCategory category);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.ApplicationModel.Contacts.ContactLocationField CreateLocation( string unstructuredAddress,  global::Windows.ApplicationModel.Contacts.ContactFieldCategory category,  string street,  string city,  string region,  string country,  string postalCode);
		#endif
	}
}
