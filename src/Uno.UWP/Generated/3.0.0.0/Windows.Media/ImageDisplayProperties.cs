#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ImageDisplayProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ImageDisplayProperties.Title is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.ImageDisplayProperties", "string ImageDisplayProperties.Title");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Subtitle
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ImageDisplayProperties.Subtitle is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.ImageDisplayProperties", "string ImageDisplayProperties.Subtitle");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.ImageDisplayProperties.Title.get
		// Forced skipping of method Windows.Media.ImageDisplayProperties.Title.set
		// Forced skipping of method Windows.Media.ImageDisplayProperties.Subtitle.get
		// Forced skipping of method Windows.Media.ImageDisplayProperties.Subtitle.set
	}
}
