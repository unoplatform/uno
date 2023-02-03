#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.PersonalInformation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactQueryResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> GetContactCountAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> ContactQueryResult.GetContactCountAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cuint%3E%20ContactQueryResult.GetContactCountAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Phone.PersonalInformation.StoredContact>> GetContactsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StoredContact>> ContactQueryResult.GetContactsAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyList%3CStoredContact%3E%3E%20ContactQueryResult.GetContactsAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Phone.PersonalInformation.StoredContact>> GetContactsAsync( uint startIndex,  uint maxNumberOfItems)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<StoredContact>> ContactQueryResult.GetContactsAsync(uint startIndex, uint maxNumberOfItems) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyList%3CStoredContact%3E%3E%20ContactQueryResult.GetContactsAsync%28uint%20startIndex%2C%20uint%20maxNumberOfItems%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Phone.PersonalInformation.ContactQueryOptions GetCurrentQueryOptions()
		{
			throw new global::System.NotImplementedException("The member ContactQueryOptions ContactQueryResult.GetCurrentQueryOptions() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ContactQueryOptions%20ContactQueryResult.GetCurrentQueryOptions%28%29");
		}
		#endif
	}
}
