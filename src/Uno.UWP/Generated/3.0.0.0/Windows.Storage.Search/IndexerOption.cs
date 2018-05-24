#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Search
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum IndexerOption 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseIndexerWhenAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnlyUseIndexer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DoNotUseIndexer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnlyUseIndexerAndOptimizeForIndexedProperties,
		#endif
	}
	#endif
}
