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
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactField> ContactInformation.CustomFields is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactField> Emails
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactField> ContactInformation.Emails is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactInstantMessageField> InstantMessages
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactInstantMessageField> ContactInformation.InstantMessages is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactLocationField> Locations
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactLocationField> ContactInformation.Locations is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactInformation.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Contacts.ContactField> PhoneNumbers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ContactField> ContactInformation.PhoneNumbers is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactInformation.Name.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IRandomAccessStreamWithContentType> GetThumbnailAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IRandomAccessStreamWithContentType> ContactInformation.GetThumbnailAsync() is not implemented in Uno.");
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
			throw new global::System.NotImplementedException("The member IReadOnlyList<ContactField> ContactInformation.QueryCustomFields(string customName) is not implemented in Uno.");
		}
		#endif
	}
}
