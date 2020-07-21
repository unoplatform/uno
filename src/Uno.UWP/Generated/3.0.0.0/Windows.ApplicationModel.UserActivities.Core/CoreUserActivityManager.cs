#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserActivities.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreUserActivityManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.UserActivities.UserActivitySession CreateUserActivitySessionInBackground( global::Windows.ApplicationModel.UserActivities.UserActivity activity)
		{
			throw new global::System.NotImplementedException("The member UserActivitySession CoreUserActivityManager.CreateUserActivitySessionInBackground(UserActivity activity) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction DeleteUserActivitySessionsInTimeRangeAsync( global::Windows.ApplicationModel.UserActivities.UserActivityChannel channel,  global::System.DateTimeOffset startTime,  global::System.DateTimeOffset endTime)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction CoreUserActivityManager.DeleteUserActivitySessionsInTimeRangeAsync(UserActivityChannel channel, DateTimeOffset startTime, DateTimeOffset endTime) is not implemented in Uno.");
		}
		#endif
	}
}
