#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactListLimitedWriteOperations 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryCreateOrUpdateContactAsync( global::Windows.ApplicationModel.Contacts.Contact contact)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ContactListLimitedWriteOperations.TryCreateOrUpdateContactAsync(Contact contact) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20ContactListLimitedWriteOperations.TryCreateOrUpdateContactAsync%28Contact%20contact%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDeleteContactAsync( string contactId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ContactListLimitedWriteOperations.TryDeleteContactAsync(string contactId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20ContactListLimitedWriteOperations.TryDeleteContactAsync%28string%20contactId%29");
		}
		#endif
	}
}
