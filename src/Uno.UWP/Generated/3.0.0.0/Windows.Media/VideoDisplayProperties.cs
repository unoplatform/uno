#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VideoDisplayProperties 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Title
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VideoDisplayProperties.Title is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoDisplayProperties", "string VideoDisplayProperties.Title");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Subtitle
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VideoDisplayProperties.Subtitle is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.VideoDisplayProperties", "string VideoDisplayProperties.Subtitle");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> Genres
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> VideoDisplayProperties.Genres is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.VideoDisplayProperties.Title.get
		// Forced skipping of method Windows.Media.VideoDisplayProperties.Title.set
		// Forced skipping of method Windows.Media.VideoDisplayProperties.Subtitle.get
		// Forced skipping of method Windows.Media.VideoDisplayProperties.Subtitle.set
		// Forced skipping of method Windows.Media.VideoDisplayProperties.Genres.get
	}
}
