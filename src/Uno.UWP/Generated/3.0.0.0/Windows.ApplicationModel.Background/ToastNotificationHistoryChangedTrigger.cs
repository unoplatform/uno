#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToastNotificationHistoryChangedTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ToastNotificationHistoryChangedTrigger( string applicationId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.ToastNotificationHistoryChangedTrigger", "ToastNotificationHistoryChangedTrigger.ToastNotificationHistoryChangedTrigger(string applicationId)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.ToastNotificationHistoryChangedTrigger.ToastNotificationHistoryChangedTrigger(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ToastNotificationHistoryChangedTrigger() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.ToastNotificationHistoryChangedTrigger", "ToastNotificationHistoryChangedTrigger.ToastNotificationHistoryChangedTrigger()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.ToastNotificationHistoryChangedTrigger.ToastNotificationHistoryChangedTrigger()
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
