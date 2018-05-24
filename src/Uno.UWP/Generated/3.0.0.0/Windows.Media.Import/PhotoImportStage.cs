#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Import
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhotoImportStage 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotStarted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FindingItems,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImportingItems,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeletingImportedItemsFromSource,
		#endif
	}
	#endif
}
