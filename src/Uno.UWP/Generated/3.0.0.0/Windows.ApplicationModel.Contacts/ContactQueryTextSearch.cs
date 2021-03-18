#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactQueryTextSearch 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Text
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactQueryTextSearch.Text is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactQueryTextSearch", "string ContactQueryTextSearch.Text");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactQuerySearchScope SearchScope
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactQuerySearchScope ContactQueryTextSearch.SearchScope is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactQueryTextSearch", "ContactQuerySearchScope ContactQueryTextSearch.SearchScope");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactQuerySearchFields Fields
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactQuerySearchFields ContactQueryTextSearch.Fields is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactQueryTextSearch", "ContactQuerySearchFields ContactQueryTextSearch.Fields");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactQueryTextSearch.Fields.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactQueryTextSearch.Fields.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactQueryTextSearch.Text.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactQueryTextSearch.Text.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactQueryTextSearch.SearchScope.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactQueryTextSearch.SearchScope.set
	}
}
