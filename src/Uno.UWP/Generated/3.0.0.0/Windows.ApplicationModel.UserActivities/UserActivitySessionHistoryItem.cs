#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserActivities
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserActivitySessionHistoryItem 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset? EndTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? UserActivitySessionHistoryItem.EndTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset StartTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset UserActivitySessionHistoryItem.StartTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.UserActivities.UserActivity UserActivity
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserActivity UserActivitySessionHistoryItem.UserActivity is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserActivities.UserActivitySessionHistoryItem.UserActivity.get
		// Forced skipping of method Windows.ApplicationModel.UserActivities.UserActivitySessionHistoryItem.StartTime.get
		// Forced skipping of method Windows.ApplicationModel.UserActivities.UserActivitySessionHistoryItem.EndTime.get
	}
}
