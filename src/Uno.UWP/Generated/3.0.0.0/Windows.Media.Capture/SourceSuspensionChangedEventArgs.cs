#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SourceSuspensionChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsAudioSuspended
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SourceSuspensionChangedEventArgs.IsAudioSuspended is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsVideoSuspended
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SourceSuspensionChangedEventArgs.IsVideoSuspended is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.SourceSuspensionChangedEventArgs.IsAudioSuspended.get
		// Forced skipping of method Windows.Media.Capture.SourceSuspensionChangedEventArgs.IsVideoSuspended.get
	}
}
