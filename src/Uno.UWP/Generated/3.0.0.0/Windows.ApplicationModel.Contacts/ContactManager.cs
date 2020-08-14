#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Contacts.ContactNameOrder SystemSortOrder
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactNameOrder ContactManager.SystemSortOrder is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactManager", "ContactNameOrder ContactManager.SystemSortOrder");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Contacts.ContactNameOrder SystemDisplayNameOrder
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactNameOrder ContactManager.SystemDisplayNameOrder is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactManager", "ContactNameOrder ContactManager.SystemDisplayNameOrder");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IncludeMiddleNameInSystemDisplayAndSort
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ContactManager.IncludeMiddleNameInSystemDisplayAndSort is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactManager", "bool ContactManager.IncludeMiddleNameInSystemDisplayAndSort");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> IsShowFullContactCardSupportedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ContactManager.IsShowFullContactCardSupportedAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactManager.IncludeMiddleNameInSystemDisplayAndSort.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactManager.IncludeMiddleNameInSystemDisplayAndSort.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Contacts.ContactManagerForUser GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member ContactManagerForUser ContactManager.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.RandomAccessStreamReference> ConvertContactToVCardAsync( global::Windows.ApplicationModel.Contacts.Contact contact)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<RandomAccessStreamReference> ContactManager.ConvertContactToVCardAsync(Contact contact) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.RandomAccessStreamReference> ConvertContactToVCardAsync( global::Windows.ApplicationModel.Contacts.Contact contact,  uint maxBytes)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<RandomAccessStreamReference> ContactManager.ConvertContactToVCardAsync(Contact contact, uint maxBytes) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Contacts.Contact> ConvertVCardToContactAsync( global::Windows.Storage.Streams.IRandomAccessStreamReference vCard)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<Contact> ContactManager.ConvertVCardToContactAsync(IRandomAccessStreamReference vCard) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Contacts.ContactStore> RequestStoreAsync( global::Windows.ApplicationModel.Contacts.ContactStoreAccessType accessType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContactStore> ContactManager.RequestStoreAsync(ContactStoreAccessType accessType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Contacts.ContactAnnotationStore> RequestAnnotationStoreAsync( global::Windows.ApplicationModel.Contacts.ContactAnnotationStoreAccessType accessType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContactAnnotationStore> ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType accessType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsShowContactCardSupported()
		{
			throw new global::System.NotImplementedException("The member bool ContactManager.IsShowContactCardSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ShowContactCard( global::Windows.ApplicationModel.Contacts.Contact contact,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement,  global::Windows.ApplicationModel.Contacts.ContactCardOptions contactCardOptions)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactManager", "void ContactManager.ShowContactCard(Contact contact, Rect selection, Placement preferredPlacement, ContactCardOptions contactCardOptions)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsShowDelayLoadedContactCardSupported()
		{
			throw new global::System.NotImplementedException("The member bool ContactManager.IsShowDelayLoadedContactCardSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Contacts.ContactCardDelayedDataLoader ShowDelayLoadedContactCard( global::Windows.ApplicationModel.Contacts.Contact contact,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement,  global::Windows.ApplicationModel.Contacts.ContactCardOptions contactCardOptions)
		{
			throw new global::System.NotImplementedException("The member ContactCardDelayedDataLoader ContactManager.ShowDelayLoadedContactCard(Contact contact, Rect selection, Placement preferredPlacement, ContactCardOptions contactCardOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ShowFullContactCard( global::Windows.ApplicationModel.Contacts.Contact contact,  global::Windows.ApplicationModel.Contacts.FullContactCardOptions fullContactCardOptions)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactManager", "void ContactManager.ShowFullContactCard(Contact contact, FullContactCardOptions fullContactCardOptions)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactManager.SystemDisplayNameOrder.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactManager.SystemDisplayNameOrder.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactManager.SystemSortOrder.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactManager.SystemSortOrder.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Contacts.ContactStore> RequestStoreAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ContactStore> ContactManager.RequestStoreAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ShowContactCard( global::Windows.ApplicationModel.Contacts.Contact contact,  global::Windows.Foundation.Rect selection)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactManager", "void ContactManager.ShowContactCard(Contact contact, Rect selection)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ShowContactCard( global::Windows.ApplicationModel.Contacts.Contact contact,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactManager", "void ContactManager.ShowContactCard(Contact contact, Rect selection, Placement preferredPlacement)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Contacts.ContactCardDelayedDataLoader ShowDelayLoadedContactCard( global::Windows.ApplicationModel.Contacts.Contact contact,  global::Windows.Foundation.Rect selection,  global::Windows.UI.Popups.Placement preferredPlacement)
		{
			throw new global::System.NotImplementedException("The member ContactCardDelayedDataLoader ContactManager.ShowDelayLoadedContactCard(Contact contact, Rect selection, Placement preferredPlacement) is not implemented in Uno.");
		}
		#endif
	}
}
