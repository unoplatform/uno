#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.StartScreen
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StartScreenManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User StartScreenManager.User is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.StartScreen.StartScreenManager.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool SupportsAppListEntry( global::Windows.ApplicationModel.Core.AppListEntry appListEntry)
		{
			throw new global::System.NotImplementedException("The member bool StartScreenManager.SupportsAppListEntry(AppListEntry appListEntry) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ContainsAppListEntryAsync( global::Windows.ApplicationModel.Core.AppListEntry appListEntry)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StartScreenManager.ContainsAppListEntryAsync(AppListEntry appListEntry) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestAddAppListEntryAsync( global::Windows.ApplicationModel.Core.AppListEntry appListEntry)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StartScreenManager.RequestAddAppListEntryAsync(AppListEntry appListEntry) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ContainsSecondaryTileAsync( string tileId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StartScreenManager.ContainsSecondaryTileAsync(string tileId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryRemoveSecondaryTileAsync( string tileId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> StartScreenManager.TryRemoveSecondaryTileAsync(string tileId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.StartScreen.StartScreenManager GetDefault()
		{
			throw new global::System.NotImplementedException("The member StartScreenManager StartScreenManager.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.StartScreen.StartScreenManager GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member StartScreenManager StartScreenManager.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
	}
}
