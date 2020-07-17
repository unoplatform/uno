#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactConnectedServiceAccount 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ServiceName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactConnectedServiceAccount.ServiceName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactConnectedServiceAccount", "string ContactConnectedServiceAccount.ServiceName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactConnectedServiceAccount.Id is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactConnectedServiceAccount", "string ContactConnectedServiceAccount.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContactConnectedServiceAccount() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactConnectedServiceAccount", "ContactConnectedServiceAccount.ContactConnectedServiceAccount()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactConnectedServiceAccount.ContactConnectedServiceAccount()
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactConnectedServiceAccount.Id.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactConnectedServiceAccount.Id.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactConnectedServiceAccount.ServiceName.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactConnectedServiceAccount.ServiceName.set
	}
}
