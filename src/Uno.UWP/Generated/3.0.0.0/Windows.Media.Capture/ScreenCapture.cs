#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ScreenCapture 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.IMediaSource AudioSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member IMediaSource ScreenCapture.AudioSource is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsAudioSuspended
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ScreenCapture.IsAudioSuspended is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsVideoSuspended
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ScreenCapture.IsVideoSuspended is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.IMediaSource VideoSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member IMediaSource ScreenCapture.VideoSource is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.ScreenCapture.AudioSource.get
		// Forced skipping of method Windows.Media.Capture.ScreenCapture.VideoSource.get
		// Forced skipping of method Windows.Media.Capture.ScreenCapture.IsAudioSuspended.get
		// Forced skipping of method Windows.Media.Capture.ScreenCapture.IsVideoSuspended.get
		// Forced skipping of method Windows.Media.Capture.ScreenCapture.SourceSuspensionChanged.add
		// Forced skipping of method Windows.Media.Capture.ScreenCapture.SourceSuspensionChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.Capture.ScreenCapture GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member ScreenCapture ScreenCapture.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.ScreenCapture, global::Windows.Media.Capture.SourceSuspensionChangedEventArgs> SourceSuspensionChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.ScreenCapture", "event TypedEventHandler<ScreenCapture, SourceSuspensionChangedEventArgs> ScreenCapture.SourceSuspensionChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.ScreenCapture", "event TypedEventHandler<ScreenCapture, SourceSuspensionChangedEventArgs> ScreenCapture.SourceSuspensionChanged");
			}
		}
		#endif
	}
}
