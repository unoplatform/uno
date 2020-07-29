#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Search
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IIndexableContent 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Id
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IDictionary<string, object> Properties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Storage.Streams.IRandomAccessStream Stream
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string StreamContentType
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Storage.Search.IIndexableContent.Id.get
		// Forced skipping of method Windows.Storage.Search.IIndexableContent.Id.set
		// Forced skipping of method Windows.Storage.Search.IIndexableContent.Properties.get
		// Forced skipping of method Windows.Storage.Search.IIndexableContent.Stream.get
		// Forced skipping of method Windows.Storage.Search.IIndexableContent.Stream.set
		// Forced skipping of method Windows.Storage.Search.IIndexableContent.StreamContentType.get
		// Forced skipping of method Windows.Storage.Search.IIndexableContent.StreamContentType.set
	}
}
