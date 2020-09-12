#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.PushNotifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PushNotificationChannel 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset ExpirationTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset PushNotificationChannel.ExpirationTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PushNotificationChannel.Uri is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationChannel.Uri.get
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationChannel.ExpirationTime.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Close()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.PushNotifications.PushNotificationChannel", "void PushNotificationChannel.Close()");
		}
		#endif
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationChannel.PushNotificationReceived.add
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationChannel.PushNotificationReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.PushNotifications.PushNotificationChannel, global::Windows.Networking.PushNotifications.PushNotificationReceivedEventArgs> PushNotificationReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.PushNotifications.PushNotificationChannel", "event TypedEventHandler<PushNotificationChannel, PushNotificationReceivedEventArgs> PushNotificationChannel.PushNotificationReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.PushNotifications.PushNotificationChannel", "event TypedEventHandler<PushNotificationChannel, PushNotificationReceivedEventArgs> PushNotificationChannel.PushNotificationReceived");
			}
		}
		#endif
	}
}
