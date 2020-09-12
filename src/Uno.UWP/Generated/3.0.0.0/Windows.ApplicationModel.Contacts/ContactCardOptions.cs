#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactCardOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactCardTabKind InitialTabKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactCardTabKind ContactCardOptions.InitialTabKind is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactCardOptions", "ContactCardTabKind ContactCardOptions.InitialTabKind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactCardHeaderKind HeaderKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactCardHeaderKind ContactCardOptions.HeaderKind is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactCardOptions", "ContactCardHeaderKind ContactCardOptions.HeaderKind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> ServerSearchContactListIds
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> ContactCardOptions.ServerSearchContactListIds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContactCardOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactCardOptions", "ContactCardOptions.ContactCardOptions()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactCardOptions.ContactCardOptions()
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactCardOptions.HeaderKind.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactCardOptions.HeaderKind.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactCardOptions.InitialTabKind.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactCardOptions.InitialTabKind.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactCardOptions.ServerSearchContactListIds.get
	}
}
