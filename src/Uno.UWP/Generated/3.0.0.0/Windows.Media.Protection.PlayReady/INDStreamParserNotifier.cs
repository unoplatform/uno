#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INDStreamParserNotifier 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void OnContentIDReceived( global::Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor licenseFetchDescriptor);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void OnMediaStreamDescriptorCreated( global::System.Collections.Generic.IList<global::Windows.Media.Core.AudioStreamDescriptor> audioStreamDescriptors,  global::System.Collections.Generic.IList<global::Windows.Media.Core.VideoStreamDescriptor> videoStreamDescriptors);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void OnSampleParsed( uint streamID,  global::Windows.Media.Protection.PlayReady.NDMediaStreamType streamType,  global::Windows.Media.Core.MediaStreamSample streamSample,  long pts,  global::Windows.Media.Protection.PlayReady.NDClosedCaptionFormat ccFormat,  byte[] ccDataBytes);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void OnBeginSetupDecryptor( global::Windows.Media.Core.IMediaStreamDescriptor descriptor,  global::System.Guid keyID,  byte[] proBytes);
		#endif
	}
}
