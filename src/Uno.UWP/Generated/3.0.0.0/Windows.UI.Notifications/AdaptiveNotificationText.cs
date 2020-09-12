#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AdaptiveNotificationText : global::Windows.UI.Notifications.IAdaptiveNotificationContent
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, string> Hints
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, string> AdaptiveNotificationText.Hints is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.AdaptiveNotificationContentKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member AdaptiveNotificationContentKind AdaptiveNotificationText.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Text
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AdaptiveNotificationText.Text is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.AdaptiveNotificationText", "string AdaptiveNotificationText.Text");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AdaptiveNotificationText.Language is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.AdaptiveNotificationText", "string AdaptiveNotificationText.Language");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AdaptiveNotificationText() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.AdaptiveNotificationText", "AdaptiveNotificationText.AdaptiveNotificationText()");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.AdaptiveNotificationText.AdaptiveNotificationText()
		// Forced skipping of method Windows.UI.Notifications.AdaptiveNotificationText.Text.get
		// Forced skipping of method Windows.UI.Notifications.AdaptiveNotificationText.Text.set
		// Forced skipping of method Windows.UI.Notifications.AdaptiveNotificationText.Language.get
		// Forced skipping of method Windows.UI.Notifications.AdaptiveNotificationText.Language.set
		// Forced skipping of method Windows.UI.Notifications.AdaptiveNotificationText.Kind.get
		// Forced skipping of method Windows.UI.Notifications.AdaptiveNotificationText.Hints.get
		// Processing: Windows.UI.Notifications.IAdaptiveNotificationContent
	}
}
