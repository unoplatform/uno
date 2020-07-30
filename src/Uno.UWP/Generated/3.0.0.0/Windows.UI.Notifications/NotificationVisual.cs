#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NotificationVisual 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NotificationVisual.Language is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.NotificationVisual", "string NotificationVisual.Language");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Notifications.NotificationBinding> Bindings
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<NotificationBinding> NotificationVisual.Bindings is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.NotificationVisual.Language.get
		// Forced skipping of method Windows.UI.Notifications.NotificationVisual.Language.set
		// Forced skipping of method Windows.UI.Notifications.NotificationVisual.Bindings.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.NotificationBinding GetBinding( string templateName)
		{
			throw new global::System.NotImplementedException("The member NotificationBinding NotificationVisual.GetBinding(string templateName) is not implemented in Uno.");
		}
		#endif
	}
}
