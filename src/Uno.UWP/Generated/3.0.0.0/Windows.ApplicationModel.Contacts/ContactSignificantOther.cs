#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactSignificantOther 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactSignificantOther.Name is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactSignificantOther", "string ContactSignificantOther.Name");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Description
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactSignificantOther.Description is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactSignificantOther", "string ContactSignificantOther.Description");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactRelationship Relationship
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactRelationship ContactSignificantOther.Relationship is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactSignificantOther", "ContactRelationship ContactSignificantOther.Relationship");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContactSignificantOther() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactSignificantOther", "ContactSignificantOther.ContactSignificantOther()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactSignificantOther.ContactSignificantOther()
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactSignificantOther.Name.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactSignificantOther.Name.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactSignificantOther.Description.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactSignificantOther.Description.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactSignificantOther.Relationship.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactSignificantOther.Relationship.set
	}
}
