#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.PersonalInformation
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong RevisionNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong ContactStore.RevisionNumber is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20ContactStore.RevisionNumber");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Phone.PersonalInformation.StoredContact> FindContactByRemoteIdAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoredContact> ContactStore.FindContactByRemoteIdAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStoredContact%3E%20ContactStore.FindContactByRemoteIdAsync%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Phone.PersonalInformation.StoredContact> FindContactByIdAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoredContact> ContactStore.FindContactByIdAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStoredContact%3E%20ContactStore.FindContactByIdAsync%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteContactAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactStore.DeleteContactAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20ContactStore.DeleteContactAsync%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Phone.PersonalInformation.ContactQueryResult CreateContactQuery()
		{
			throw new global::System.NotImplementedException("The member ContactQueryResult ContactStore.CreateContactQuery() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ContactQueryResult%20ContactStore.CreateContactQuery%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Phone.PersonalInformation.ContactQueryResult CreateContactQuery( global::Windows.Phone.PersonalInformation.ContactQueryOptions options)
		{
			throw new global::System.NotImplementedException("The member ContactQueryResult ContactStore.CreateContactQuery(ContactQueryOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ContactQueryResult%20ContactStore.CreateContactQuery%28ContactQueryOptions%20options%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactStore.DeleteAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20ContactStore.DeleteAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.Phone.PersonalInformation.ContactStore.RevisionNumber.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Phone.PersonalInformation.ContactChangeRecord>> GetChangesAsync( ulong baseRevisionNumber)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ContactChangeRecord>> ContactStore.GetChangesAsync(ulong baseRevisionNumber) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyList%3CContactChangeRecord%3E%3E%20ContactStore.GetChangesAsync%28ulong%20baseRevisionNumber%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IDictionary<string, object>> LoadExtendedPropertiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IDictionary<string, object>> ContactStore.LoadExtendedPropertiesAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIDictionary%3Cstring%2C%20object%3E%3E%20ContactStore.LoadExtendedPropertiesAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveExtendedPropertiesAsync( global::System.Collections.Generic.IReadOnlyDictionary<string, object> data)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactStore.SaveExtendedPropertiesAsync(IReadOnlyDictionary<string, object> data) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20ContactStore.SaveExtendedPropertiesAsync%28IReadOnlyDictionary%3Cstring%2C%20object%3E%20data%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Phone.PersonalInformation.StoredContact> CreateMeContactAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<StoredContact> ContactStore.CreateMeContactAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CStoredContact%3E%20ContactStore.CreateMeContactAsync%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Phone.PersonalInformation.ContactStore> CreateOrOpenAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContactStore> ContactStore.CreateOrOpenAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CContactStore%3E%20ContactStore.CreateOrOpenAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Phone.PersonalInformation.ContactStore> CreateOrOpenAsync( global::Windows.Phone.PersonalInformation.ContactStoreSystemAccessMode access,  global::Windows.Phone.PersonalInformation.ContactStoreApplicationAccessMode sharing)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContactStore> ContactStore.CreateOrOpenAsync(ContactStoreSystemAccessMode access, ContactStoreApplicationAccessMode sharing) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CContactStore%3E%20ContactStore.CreateOrOpenAsync%28ContactStoreSystemAccessMode%20access%2C%20ContactStoreApplicationAccessMode%20sharing%29");
		}
		#endif
	}
}
