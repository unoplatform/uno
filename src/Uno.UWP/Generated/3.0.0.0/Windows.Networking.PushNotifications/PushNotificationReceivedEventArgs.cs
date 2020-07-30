#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.PushNotifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PushNotificationReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Cancel
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PushNotificationReceivedEventArgs.Cancel is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs", "bool PushNotificationReceivedEventArgs.Cancel");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.BadgeNotification BadgeNotification
		{
			get
			{
				throw new global::System.NotImplementedException("The member BadgeNotification PushNotificationReceivedEventArgs.BadgeNotification is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.PushNotifications.PushNotificationType NotificationType
		{
			get
			{
				throw new global::System.NotImplementedException("The member PushNotificationType PushNotificationReceivedEventArgs.NotificationType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.PushNotifications.RawNotification RawNotification
		{
			get
			{
				throw new global::System.NotImplementedException("The member RawNotification PushNotificationReceivedEventArgs.RawNotification is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.TileNotification TileNotification
		{
			get
			{
				throw new global::System.NotImplementedException("The member TileNotification PushNotificationReceivedEventArgs.TileNotification is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.ToastNotification ToastNotification
		{
			get
			{
				throw new global::System.NotImplementedException("The member ToastNotification PushNotificationReceivedEventArgs.ToastNotification is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs.Cancel.set
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs.Cancel.get
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs.NotificationType.get
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs.ToastNotification.get
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs.TileNotification.get
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs.BadgeNotification.get
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs.RawNotification.get
	}
}
