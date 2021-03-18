#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Notification 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.NotificationVisual Visual
		{
			get
			{
				throw new global::System.NotImplementedException("The member NotificationVisual Notification.Visual is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.Notification", "NotificationVisual Notification.Visual");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset? ExpirationTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? Notification.ExpirationTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.Notification", "DateTimeOffset? Notification.ExpirationTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public Notification() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.Notification", "Notification.Notification()");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.Notification.Notification()
		// Forced skipping of method Windows.UI.Notifications.Notification.ExpirationTime.get
		// Forced skipping of method Windows.UI.Notifications.Notification.ExpirationTime.set
		// Forced skipping of method Windows.UI.Notifications.Notification.Visual.get
		// Forced skipping of method Windows.UI.Notifications.Notification.Visual.set
	}
}
