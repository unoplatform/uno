#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ServiceRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.MediaProtectionServiceCompletion Completion
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaProtectionServiceCompletion ServiceRequestedEventArgs.Completion is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaProtectionServiceCompletion%20ServiceRequestedEventArgs.Completion");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.IMediaProtectionServiceRequest Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member IMediaProtectionServiceRequest ServiceRequestedEventArgs.Request is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IMediaProtectionServiceRequest%20ServiceRequestedEventArgs.Request");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Playback.MediaPlaybackItem MediaPlaybackItem
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaPlaybackItem ServiceRequestedEventArgs.MediaPlaybackItem is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MediaPlaybackItem%20ServiceRequestedEventArgs.MediaPlaybackItem");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.ServiceRequestedEventArgs.Request.get
		// Forced skipping of method Windows.Media.Protection.ServiceRequestedEventArgs.Completion.get
		// Forced skipping of method Windows.Media.Protection.ServiceRequestedEventArgs.MediaPlaybackItem.get
	}
}
