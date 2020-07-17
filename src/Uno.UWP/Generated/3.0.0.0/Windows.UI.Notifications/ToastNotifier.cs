#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToastNotifier 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.NotificationSetting Setting
		{
			get
			{
				throw new global::System.NotImplementedException("The member NotificationSetting ToastNotifier.Setting is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Show( global::Windows.UI.Notifications.ToastNotification notification)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotifier", "void ToastNotifier.Show(ToastNotification notification)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Hide( global::Windows.UI.Notifications.ToastNotification notification)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotifier", "void ToastNotifier.Hide(ToastNotification notification)");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.ToastNotifier.Setting.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AddToSchedule( global::Windows.UI.Notifications.ScheduledToastNotification scheduledToast)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotifier", "void ToastNotifier.AddToSchedule(ScheduledToastNotification scheduledToast)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveFromSchedule( global::Windows.UI.Notifications.ScheduledToastNotification scheduledToast)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotifier", "void ToastNotifier.RemoveFromSchedule(ScheduledToastNotification scheduledToast)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.UI.Notifications.ScheduledToastNotification> GetScheduledToastNotifications()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ScheduledToastNotification> ToastNotifier.GetScheduledToastNotifications() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.NotificationUpdateResult Update( global::Windows.UI.Notifications.NotificationData data,  string tag,  string group)
		{
			throw new global::System.NotImplementedException("The member NotificationUpdateResult ToastNotifier.Update(NotificationData data, string tag, string group) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.NotificationUpdateResult Update( global::Windows.UI.Notifications.NotificationData data,  string tag)
		{
			throw new global::System.NotImplementedException("The member NotificationUpdateResult ToastNotifier.Update(NotificationData data, string tag) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.ToastNotifier.ScheduledToastNotificationShowing.add
		// Forced skipping of method Windows.UI.Notifications.ToastNotifier.ScheduledToastNotificationShowing.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Notifications.ToastNotifier, global::Windows.UI.Notifications.ScheduledToastNotificationShowingEventArgs> ScheduledToastNotificationShowing
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotifier", "event TypedEventHandler<ToastNotifier, ScheduledToastNotificationShowingEventArgs> ToastNotifier.ScheduledToastNotificationShowing");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.ToastNotifier", "event TypedEventHandler<ToastNotifier, ScheduledToastNotificationShowingEventArgs> ToastNotifier.ScheduledToastNotificationShowing");
			}
		}
		#endif
	}
}
