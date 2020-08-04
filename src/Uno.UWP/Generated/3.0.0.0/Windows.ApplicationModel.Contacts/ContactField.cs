#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactField : global::Windows.ApplicationModel.Contacts.IContactField
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactFieldCategory Category
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactFieldCategory ContactField.Category is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactField.Name is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactFieldType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactFieldType ContactField.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactField.Value is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContactField( string value,  global::Windows.ApplicationModel.Contacts.ContactFieldType type) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactField", "ContactField.ContactField(string value, ContactFieldType type)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactField.ContactField(string, Windows.ApplicationModel.Contacts.ContactFieldType)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContactField( string value,  global::Windows.ApplicationModel.Contacts.ContactFieldType type,  global::Windows.ApplicationModel.Contacts.ContactFieldCategory category) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactField", "ContactField.ContactField(string value, ContactFieldType type, ContactFieldCategory category)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactField.ContactField(string, Windows.ApplicationModel.Contacts.ContactFieldType, Windows.ApplicationModel.Contacts.ContactFieldCategory)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ContactField( string name,  string value,  global::Windows.ApplicationModel.Contacts.ContactFieldType type,  global::Windows.ApplicationModel.Contacts.ContactFieldCategory category) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactField", "ContactField.ContactField(string name, string value, ContactFieldType type, ContactFieldCategory category)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactField.ContactField(string, string, Windows.ApplicationModel.Contacts.ContactFieldType, Windows.ApplicationModel.Contacts.ContactFieldCategory)
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactField.Type.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactField.Category.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactField.Name.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactField.Value.get
		// Processing: Windows.ApplicationModel.Contacts.IContactField
	}
}
