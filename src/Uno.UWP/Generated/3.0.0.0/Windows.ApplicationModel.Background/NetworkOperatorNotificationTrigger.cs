#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NetworkOperatorNotificationTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string NetworkAccountId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string NetworkOperatorNotificationTrigger.NetworkAccountId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NetworkOperatorNotificationTrigger( string networkAccountId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.NetworkOperatorNotificationTrigger", "NetworkOperatorNotificationTrigger.NetworkOperatorNotificationTrigger(string networkAccountId)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.NetworkOperatorNotificationTrigger.NetworkOperatorNotificationTrigger(string)
		// Forced skipping of method Windows.ApplicationModel.Background.NetworkOperatorNotificationTrigger.NetworkAccountId.get
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
