#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.PushNotifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PushNotificationChannelManager 
	{
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationChannelManager.ChannelsRevoked.add
		// Forced skipping of method Windows.Networking.PushNotifications.PushNotificationChannelManager.ChannelsRevoked.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.PushNotifications.PushNotificationChannelManagerForUser GetDefault()
		{
			throw new global::System.NotImplementedException("The member PushNotificationChannelManagerForUser PushNotificationChannelManager.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Networking.PushNotifications.PushNotificationChannelManagerForUser GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member PushNotificationChannelManagerForUser PushNotificationChannelManager.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.PushNotifications.PushNotificationChannel> CreatePushNotificationChannelForApplicationAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PushNotificationChannel> PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.PushNotifications.PushNotificationChannel> CreatePushNotificationChannelForApplicationAsync( string applicationId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PushNotificationChannel> PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync(string applicationId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.PushNotifications.PushNotificationChannel> CreatePushNotificationChannelForSecondaryTileAsync( string tileId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<PushNotificationChannel> PushNotificationChannelManager.CreatePushNotificationChannelForSecondaryTileAsync(string tileId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Networking.PushNotifications.PushNotificationChannelsRevokedEventArgs> ChannelsRevoked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.PushNotifications.PushNotificationChannelManager", "event EventHandler<PushNotificationChannelsRevokedEventArgs> PushNotificationChannelManager.ChannelsRevoked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.PushNotifications.PushNotificationChannelManager", "event EventHandler<PushNotificationChannelsRevokedEventArgs> PushNotificationChannelManager.ChannelsRevoked");
			}
		}
		#endif
	}
}
