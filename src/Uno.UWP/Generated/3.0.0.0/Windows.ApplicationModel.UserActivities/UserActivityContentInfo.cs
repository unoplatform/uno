#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserActivities
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserActivityContentInfo : global::Windows.ApplicationModel.UserActivities.IUserActivityContentInfo
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ToJson()
		{
			throw new global::System.NotImplementedException("The member string UserActivityContentInfo.ToJson() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20UserActivityContentInfo.ToJson%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.UserActivities.UserActivityContentInfo FromJson( string value)
		{
			throw new global::System.NotImplementedException("The member UserActivityContentInfo UserActivityContentInfo.FromJson(string value) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserActivityContentInfo%20UserActivityContentInfo.FromJson%28string%20value%29");
		}
		#endif
		// Processing: Windows.ApplicationModel.UserActivities.IUserActivityContentInfo
	}
}
