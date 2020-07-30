#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TileFlyoutUpdater 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.NotificationSetting Setting
		{
			get
			{
				throw new global::System.NotImplementedException("The member NotificationSetting TileFlyoutUpdater.Setting is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Update( global::Windows.UI.Notifications.TileFlyoutNotification notification)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.TileFlyoutUpdater", "void TileFlyoutUpdater.Update(TileFlyoutNotification notification)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Clear()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.TileFlyoutUpdater", "void TileFlyoutUpdater.Clear()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartPeriodicUpdate( global::System.Uri tileFlyoutContent,  global::Windows.UI.Notifications.PeriodicUpdateRecurrence requestedInterval)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.TileFlyoutUpdater", "void TileFlyoutUpdater.StartPeriodicUpdate(Uri tileFlyoutContent, PeriodicUpdateRecurrence requestedInterval)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartPeriodicUpdate( global::System.Uri tileFlyoutContent,  global::System.DateTimeOffset startTime,  global::Windows.UI.Notifications.PeriodicUpdateRecurrence requestedInterval)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.TileFlyoutUpdater", "void TileFlyoutUpdater.StartPeriodicUpdate(Uri tileFlyoutContent, DateTimeOffset startTime, PeriodicUpdateRecurrence requestedInterval)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StopPeriodicUpdate()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Notifications.TileFlyoutUpdater", "void TileFlyoutUpdater.StopPeriodicUpdate()");
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.TileFlyoutUpdater.Setting.get
	}
}
