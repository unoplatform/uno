#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Search
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class IndexableContent : global::Windows.Storage.Search.IIndexableContent
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string StreamContentType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string IndexableContent.StreamContentType is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Search.IndexableContent", "string IndexableContent.StreamContentType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IRandomAccessStream Stream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IRandomAccessStream IndexableContent.Stream is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Search.IndexableContent", "IRandomAccessStream IndexableContent.Stream");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string IndexableContent.Id is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Search.IndexableContent", "string IndexableContent.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, object> IndexableContent.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public IndexableContent() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Storage.Search.IndexableContent", "IndexableContent.IndexableContent()");
		}
		#endif
		// Forced skipping of method Windows.Storage.Search.IndexableContent.IndexableContent()
		// Forced skipping of method Windows.Storage.Search.IndexableContent.Id.get
		// Forced skipping of method Windows.Storage.Search.IndexableContent.Id.set
		// Forced skipping of method Windows.Storage.Search.IndexableContent.Properties.get
		// Forced skipping of method Windows.Storage.Search.IndexableContent.Stream.get
		// Forced skipping of method Windows.Storage.Search.IndexableContent.Stream.set
		// Forced skipping of method Windows.Storage.Search.IndexableContent.StreamContentType.get
		// Forced skipping of method Windows.Storage.Search.IndexableContent.StreamContentType.set
		// Processing: Windows.Storage.Search.IIndexableContent
	}
}
