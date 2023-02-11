#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserNotification 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.AppInfo AppInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppInfo UserNotification.AppInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppInfo%20UserNotification.AppInfo");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset CreationTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset UserNotification.CreationTime is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DateTimeOffset%20UserNotification.CreationTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UserNotification.Id is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20UserNotification.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.Notification Notification
		{
			get
			{
				throw new global::System.NotImplementedException("The member Notification UserNotification.Notification is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Notification%20UserNotification.Notification");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.UserNotification.Notification.get
		// Forced skipping of method Windows.UI.Notifications.UserNotification.AppInfo.get
		// Forced skipping of method Windows.UI.Notifications.UserNotification.Id.get
		// Forced skipping of method Windows.UI.Notifications.UserNotification.CreationTime.get
	}
}
