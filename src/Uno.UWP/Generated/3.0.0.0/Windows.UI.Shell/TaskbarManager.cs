#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Shell
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TaskbarManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPinningAllowed
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool TaskbarManager.IsPinningAllowed is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool TaskbarManager.IsSupported is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Shell.TaskbarManager.IsSupported.get
		// Forced skipping of method Windows.UI.Shell.TaskbarManager.IsPinningAllowed.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> IsCurrentAppPinnedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> TaskbarManager.IsCurrentAppPinnedAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> IsAppListEntryPinnedAsync( global::Windows.ApplicationModel.Core.AppListEntry appListEntry)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> TaskbarManager.IsAppListEntryPinnedAsync(AppListEntry appListEntry) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestPinCurrentAppAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> TaskbarManager.RequestPinCurrentAppAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestPinAppListEntryAsync( global::Windows.ApplicationModel.Core.AppListEntry appListEntry)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> TaskbarManager.RequestPinAppListEntryAsync(AppListEntry appListEntry) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> IsSecondaryTilePinnedAsync( string tileId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> TaskbarManager.IsSecondaryTilePinnedAsync(string tileId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> RequestPinSecondaryTileAsync( global::Windows.UI.StartScreen.SecondaryTile secondaryTile)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> TaskbarManager.RequestPinSecondaryTileAsync(SecondaryTile secondaryTile) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryUnpinSecondaryTileAsync( string tileId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> TaskbarManager.TryUnpinSecondaryTileAsync(string tileId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Shell.TaskbarManager GetDefault()
		{
			throw new global::System.NotImplementedException("The member TaskbarManager TaskbarManager.GetDefault() is not implemented in Uno.");
		}
		#endif
	}
}
