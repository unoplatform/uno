#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactField> CustomFields
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactField> ContactInformation.CustomFields is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CContactField%3E%20ContactInformation.CustomFields");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactField> Emails
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactField> ContactInformation.Emails is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CContactField%3E%20ContactInformation.Emails");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactInstantMessageField> InstantMessages
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactInstantMessageField> ContactInformation.InstantMessages is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CContactInstantMessageField%3E%20ContactInformation.InstantMessages");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactLocationField> Locations
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactLocationField> ContactInformation.Locations is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CContactLocationField%3E%20ContactInformation.Locations");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactInformation.Name is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ContactInformation.Name");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactField> PhoneNumbers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactField> ContactInformation.PhoneNumbers is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CContactField%3E%20ContactInformation.PhoneNumbers");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactInformation.Name.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStreamWithContentType> GetThumbnailAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStreamWithContentType> ContactInformation.GetThumbnailAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIRandomAccessStreamWithContentType%3E%20ContactInformation.GetThumbnailAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactInformation.Emails.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactInformation.PhoneNumbers.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactInformation.Locations.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactInformation.InstantMessages.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactInformation.CustomFields.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactField> QueryCustomFields( string customName)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ContactField> ContactInformation.QueryCustomFields(string customName) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CContactField%3E%20ContactInformation.QueryCustomFields%28string%20customName%29");
		}
		#endif
	}
}
