#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactListCreateOrUpdateContactRequestEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.DataProvider.ContactListCreateOrUpdateContactRequest Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactListCreateOrUpdateContactRequest ContactListCreateOrUpdateContactRequestEventArgs.Request is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ContactListCreateOrUpdateContactRequest%20ContactListCreateOrUpdateContactRequestEventArgs.Request");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.DataProvider.ContactListCreateOrUpdateContactRequestEventArgs.Request.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral ContactListCreateOrUpdateContactRequestEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20ContactListCreateOrUpdateContactRequestEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
