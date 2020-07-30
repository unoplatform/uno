#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaBreak 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanStart
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MediaBreak.CanStart is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreak", "bool MediaBreak.CanStart");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.ValueSet CustomProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet MediaBreak.CustomProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaBreakInsertionMethod InsertionMethod
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaBreakInsertionMethod MediaBreak.InsertionMethod is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackList PlaybackList
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackList MediaBreak.PlaybackList is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? PresentationPosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MediaBreak.PresentationPosition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MediaBreak( global::Windows.Media.Playback.MediaBreakInsertionMethod insertionMethod) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreak", "MediaBreak.MediaBreak(MediaBreakInsertionMethod insertionMethod)");
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaBreak.MediaBreak(Windows.Media.Playback.MediaBreakInsertionMethod)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MediaBreak( global::Windows.Media.Playback.MediaBreakInsertionMethod insertionMethod,  global::System.TimeSpan presentationPosition) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Playback.MediaBreak", "MediaBreak.MediaBreak(MediaBreakInsertionMethod insertionMethod, TimeSpan presentationPosition)");
		}
		#endif
		// Forced skipping of method Windows.Media.Playback.MediaBreak.MediaBreak(Windows.Media.Playback.MediaBreakInsertionMethod, System.TimeSpan)
		// Forced skipping of method Windows.Media.Playback.MediaBreak.PlaybackList.get
		// Forced skipping of method Windows.Media.Playback.MediaBreak.PresentationPosition.get
		// Forced skipping of method Windows.Media.Playback.MediaBreak.InsertionMethod.get
		// Forced skipping of method Windows.Media.Playback.MediaBreak.CustomProperties.get
		// Forced skipping of method Windows.Media.Playback.MediaBreak.CanStart.get
		// Forced skipping of method Windows.Media.Playback.MediaBreak.CanStart.set
	}
}
