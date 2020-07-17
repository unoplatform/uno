#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IAdaptiveNotificationContent 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IDictionary<string, string> Hints
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Notifications.AdaptiveNotificationContentKind Kind
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.IAdaptiveNotificationContent.Kind.get
		// Forced skipping of method Windows.UI.Notifications.IAdaptiveNotificationContent.Hints.get
	}
}
