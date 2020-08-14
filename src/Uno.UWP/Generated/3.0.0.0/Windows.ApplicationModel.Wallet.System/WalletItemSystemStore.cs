#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Wallet.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WalletItemSystemStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Wallet.WalletItem>> GetItemsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<WalletItem>> WalletItemSystemStore.GetItemsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteAsync( global::Windows.ApplicationModel.Wallet.WalletItem item)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WalletItemSystemStore.DeleteAsync(WalletItem item) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Wallet.WalletItem> ImportItemAsync( global::Windows.Storage.Streams.IRandomAccessStreamReference stream)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<WalletItem> WalletItemSystemStore.ImportItemAsync(IRandomAccessStreamReference stream) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Wallet.System.WalletItemAppAssociation GetAppStatusForItem( global::Windows.ApplicationModel.Wallet.WalletItem item)
		{
			throw new global::System.NotImplementedException("The member WalletItemAppAssociation WalletItemSystemStore.GetAppStatusForItem(WalletItem item) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> LaunchAppForItemAsync( global::Windows.ApplicationModel.Wallet.WalletItem item)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> WalletItemSystemStore.LaunchAppForItemAsync(WalletItem item) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Wallet.System.WalletItemSystemStore.ItemsChanged.add
		// Forced skipping of method Windows.ApplicationModel.Wallet.System.WalletItemSystemStore.ItemsChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Wallet.System.WalletItemSystemStore, object> ItemsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Wallet.System.WalletItemSystemStore", "event TypedEventHandler<WalletItemSystemStore, object> WalletItemSystemStore.ItemsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Wallet.System.WalletItemSystemStore", "event TypedEventHandler<WalletItemSystemStore, object> WalletItemSystemStore.ItemsChanged");
			}
		}
		#endif
	}
}
