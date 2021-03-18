#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactPanelLaunchFullAppRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ContactPanelLaunchFullAppRequestedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Contacts.ContactPanelLaunchFullAppRequestedEventArgs", "bool ContactPanelLaunchFullAppRequestedEventArgs.Handled");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactPanelLaunchFullAppRequestedEventArgs.Handled.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.ContactPanelLaunchFullAppRequestedEventArgs.Handled.set
	}
}
