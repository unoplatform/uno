#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Search
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CommonFolderQuery 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DefaultQuery,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByYear,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByMonth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByArtist,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByAlbum,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByAlbumArtist,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByComposer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByGenre,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByPublishedYear,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByRating,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByTag,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByAuthor,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupByType,
		#endif
	}
	#endif
}
