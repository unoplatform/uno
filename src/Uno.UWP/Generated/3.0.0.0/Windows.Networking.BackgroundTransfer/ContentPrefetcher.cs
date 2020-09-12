#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContentPrefetcher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Uri IndirectContentUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri ContentPrefetcher.IndirectContentUri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.BackgroundTransfer.ContentPrefetcher", "Uri ContentPrefetcher.IndirectContentUri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IList<global::System.Uri> ContentUris
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<Uri> ContentPrefetcher.ContentUris is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.DateTimeOffset? LastSuccessfulPrefetchTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? ContentPrefetcher.LastSuccessfulPrefetchTime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.ContentPrefetcher.LastSuccessfulPrefetchTime.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.ContentPrefetcher.ContentUris.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.ContentPrefetcher.IndirectContentUri.set
		// Forced skipping of method Windows.Networking.BackgroundTransfer.ContentPrefetcher.IndirectContentUri.get
	}
}
