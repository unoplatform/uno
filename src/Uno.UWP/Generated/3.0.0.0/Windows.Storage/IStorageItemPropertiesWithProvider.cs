#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStorageItemPropertiesWithProvider : global::Windows.Storage.IStorageItemProperties
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.StorageProvider Provider
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Storage.IStorageItemPropertiesWithProvider.Provider.get
	}
}
