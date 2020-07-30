#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaStreamSourceSwitchStreamsRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.IMediaStreamDescriptor NewStreamDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member IMediaStreamDescriptor MediaStreamSourceSwitchStreamsRequest.NewStreamDescriptor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.IMediaStreamDescriptor OldStreamDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member IMediaStreamDescriptor MediaStreamSourceSwitchStreamsRequest.OldStreamDescriptor is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaStreamSourceSwitchStreamsRequest.OldStreamDescriptor.get
		// Forced skipping of method Windows.Media.Core.MediaStreamSourceSwitchStreamsRequest.NewStreamDescriptor.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaStreamSourceSwitchStreamsRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member MediaStreamSourceSwitchStreamsRequestDeferral MediaStreamSourceSwitchStreamsRequest.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
