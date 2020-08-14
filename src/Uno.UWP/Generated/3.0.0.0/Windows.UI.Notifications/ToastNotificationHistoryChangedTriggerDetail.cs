#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToastNotificationHistoryChangedTriggerDetail 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Notifications.ToastHistoryChangedType ChangeType
		{
			get
			{
				throw new global::System.NotImplementedException("The member ToastHistoryChangedType ToastNotificationHistoryChangedTriggerDetail.ChangeType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CollectionId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ToastNotificationHistoryChangedTriggerDetail.CollectionId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.ToastNotificationHistoryChangedTriggerDetail.ChangeType.get
		// Forced skipping of method Windows.UI.Notifications.ToastNotificationHistoryChangedTriggerDetail.CollectionId.get
	}
}
