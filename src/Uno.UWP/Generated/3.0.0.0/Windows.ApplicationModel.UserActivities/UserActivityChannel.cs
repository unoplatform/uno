#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserActivities
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserActivityChannel 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserActivities.UserActivity> GetOrCreateUserActivityAsync( string activityId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserActivity> UserActivityChannel.GetOrCreateUserActivityAsync(string activityId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteActivityAsync( string activityId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UserActivityChannel.DeleteActivityAsync(string activityId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteAllActivitiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UserActivityChannel.DeleteAllActivitiesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.ApplicationModel.UserActivities.UserActivitySessionHistoryItem>> GetRecentUserActivitiesAsync( int maxUniqueActivities)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<UserActivitySessionHistoryItem>> UserActivityChannel.GetRecentUserActivitiesAsync(int maxUniqueActivities) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IList<global::Windows.ApplicationModel.UserActivities.UserActivitySessionHistoryItem>> GetSessionHistoryItemsForUserActivityAsync( string activityId,  global::System.DateTimeOffset startTime)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IList<UserActivitySessionHistoryItem>> UserActivityChannel.GetSessionHistoryItemsForUserActivityAsync(string activityId, DateTimeOffset startTime) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.UserActivities.UserActivityChannel GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member UserActivityChannel UserActivityChannel.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void DisableAutoSessionCreation()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserActivities.UserActivityChannel", "void UserActivityChannel.DisableAutoSessionCreation()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.UserActivities.UserActivityChannel TryGetForWebAccount( global::Windows.Security.Credentials.WebAccount account)
		{
			throw new global::System.NotImplementedException("The member UserActivityChannel UserActivityChannel.TryGetForWebAccount(WebAccount account) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.UserActivities.UserActivityChannel GetDefault()
		{
			throw new global::System.NotImplementedException("The member UserActivityChannel UserActivityChannel.GetDefault() is not implemented in Uno.");
		}
		#endif
	}
}
