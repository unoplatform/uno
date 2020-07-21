#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Search
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentIndexerQuery 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.StorageFolder QueryFolder
		{
			get
			{
				throw new global::System.NotImplementedException("The member StorageFolder ContentIndexerQuery.QueryFolder is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> GetCountAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> ContentIndexerQuery.GetCountAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.IReadOnlyDictionary<string, object>>> GetPropertiesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IReadOnlyDictionary<string, object>>> ContentIndexerQuery.GetPropertiesAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::System.Collections.Generic.IReadOnlyDictionary<string, object>>> GetPropertiesAsync( uint startIndex,  uint maxItems)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IReadOnlyDictionary<string, object>>> ContentIndexerQuery.GetPropertiesAsync(uint startIndex, uint maxItems) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.Search.IIndexableContent>> GetAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IIndexableContent>> ContentIndexerQuery.GetAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Storage.Search.IIndexableContent>> GetAsync( uint startIndex,  uint maxItems)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<IIndexableContent>> ContentIndexerQuery.GetAsync(uint startIndex, uint maxItems) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Storage.Search.ContentIndexerQuery.QueryFolder.get
	}
}
