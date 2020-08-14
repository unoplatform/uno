#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactWebsite 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri ContactWebsite.Uri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactWebsite", "Uri ContactWebsite.Uri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Description
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactWebsite.Description is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactWebsite", "string ContactWebsite.Description");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string RawValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactWebsite.RawValue is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactWebsite", "string ContactWebsite.RawValue");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContactWebsite() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactWebsite", "ContactWebsite.ContactWebsite()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactWebsite.ContactWebsite()
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactWebsite.Uri.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactWebsite.Uri.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactWebsite.Description.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactWebsite.Description.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactWebsite.RawValue.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactWebsite.RawValue.set
	}
}
