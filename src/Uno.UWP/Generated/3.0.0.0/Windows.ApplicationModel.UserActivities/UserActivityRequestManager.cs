#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserActivities
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserActivityRequestManager 
	{
		// Forced skipping of method Windows.ApplicationModel.UserActivities.UserActivityRequestManager.UserActivityRequested.add
		// Forced skipping of method Windows.ApplicationModel.UserActivities.UserActivityRequestManager.UserActivityRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.UserActivities.UserActivityRequestManager GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member UserActivityRequestManager UserActivityRequestManager.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.UserActivities.UserActivityRequestManager, global::Windows.ApplicationModel.UserActivities.UserActivityRequestedEventArgs> UserActivityRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserActivities.UserActivityRequestManager", "event TypedEventHandler<UserActivityRequestManager, UserActivityRequestedEventArgs> UserActivityRequestManager.UserActivityRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserActivities.UserActivityRequestManager", "event TypedEventHandler<UserActivityRequestManager, UserActivityRequestedEventArgs> UserActivityRequestManager.UserActivityRequested");
			}
		}
		#endif
	}
}
