#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserNotificationChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.UserNotificationChangedKind ChangeKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserNotificationChangedKind UserNotificationChangedEventArgs.ChangeKind is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserNotificationChangedKind%20UserNotificationChangedEventArgs.ChangeKind");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint UserNotificationId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UserNotificationChangedEventArgs.UserNotificationId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20UserNotificationChangedEventArgs.UserNotificationId");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.UserNotificationChangedEventArgs.ChangeKind.get
		// Forced skipping of method Windows.UI.Notifications.UserNotificationChangedEventArgs.UserNotificationId.get
	}
}
