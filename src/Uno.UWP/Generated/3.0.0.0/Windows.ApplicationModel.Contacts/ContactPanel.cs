#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactPanel 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Color? HeaderColor
		{
			get
			{
				throw new global::System.NotImplementedException("The member Color? ContactPanel.HeaderColor is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactPanel", "Color? ContactPanel.HeaderColor");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ClosePanel()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactPanel", "void ContactPanel.ClosePanel()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactPanel.HeaderColor.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactPanel.HeaderColor.set
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactPanel.LaunchFullAppRequested.add
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactPanel.LaunchFullAppRequested.remove
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactPanel.Closing.add
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactPanel.Closing.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Contacts.ContactPanel, global::Windows.ApplicationModel.Contacts.ContactPanelClosingEventArgs> Closing
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactPanel", "event TypedEventHandler<ContactPanel, ContactPanelClosingEventArgs> ContactPanel.Closing");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactPanel", "event TypedEventHandler<ContactPanel, ContactPanelClosingEventArgs> ContactPanel.Closing");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Contacts.ContactPanel, global::Windows.ApplicationModel.Contacts.ContactPanelLaunchFullAppRequestedEventArgs> LaunchFullAppRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactPanel", "event TypedEventHandler<ContactPanel, ContactPanelLaunchFullAppRequestedEventArgs> ContactPanel.LaunchFullAppRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactPanel", "event TypedEventHandler<ContactPanel, ContactPanelLaunchFullAppRequestedEventArgs> ContactPanel.LaunchFullAppRequested");
			}
		}
		#endif
	}
}
