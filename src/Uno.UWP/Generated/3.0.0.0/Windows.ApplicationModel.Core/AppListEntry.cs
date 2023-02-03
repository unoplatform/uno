#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppListEntry 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.AppDisplayInfo DisplayInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppDisplayInfo AppListEntry.DisplayInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppDisplayInfo%20AppListEntry.DisplayInfo");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AppUserModelId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppListEntry.AppUserModelId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppListEntry.AppUserModelId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.AppInfo AppInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppInfo AppListEntry.AppInfo is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AppInfo%20AppListEntry.AppInfo");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.AppListEntry.DisplayInfo.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> LaunchAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppListEntry.LaunchAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20AppListEntry.LaunchAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.AppListEntry.AppUserModelId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> LaunchForUserAsync( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> AppListEntry.LaunchForUserAsync(User user) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20AppListEntry.LaunchForUserAsync%28User%20user%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Core.AppListEntry.AppInfo.get
	}
}
