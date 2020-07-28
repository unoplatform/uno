#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NDStreamParserNotifier : global::Windows.Media.Protection.PlayReady.INDStreamParserNotifier
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public NDStreamParserNotifier() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDStreamParserNotifier", "NDStreamParserNotifier.NDStreamParserNotifier()");
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.PlayReady.NDStreamParserNotifier.NDStreamParserNotifier()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void OnContentIDReceived( global::Windows.Media.Protection.PlayReady.INDLicenseFetchDescriptor licenseFetchDescriptor)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDStreamParserNotifier", "void NDStreamParserNotifier.OnContentIDReceived(INDLicenseFetchDescriptor licenseFetchDescriptor)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void OnMediaStreamDescriptorCreated( global::System.Collections.Generic.IList<global::Windows.Media.Core.AudioStreamDescriptor> audioStreamDescriptors,  global::System.Collections.Generic.IList<global::Windows.Media.Core.VideoStreamDescriptor> videoStreamDescriptors)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDStreamParserNotifier", "void NDStreamParserNotifier.OnMediaStreamDescriptorCreated(IList<AudioStreamDescriptor> audioStreamDescriptors, IList<VideoStreamDescriptor> videoStreamDescriptors)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void OnSampleParsed( uint streamID,  global::Windows.Media.Protection.PlayReady.NDMediaStreamType streamType,  global::Windows.Media.Core.MediaStreamSample streamSample,  long pts,  global::Windows.Media.Protection.PlayReady.NDClosedCaptionFormat ccFormat,  byte[] ccDataBytes)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDStreamParserNotifier", "void NDStreamParserNotifier.OnSampleParsed(uint streamID, NDMediaStreamType streamType, MediaStreamSample streamSample, long pts, NDClosedCaptionFormat ccFormat, byte[] ccDataBytes)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void OnBeginSetupDecryptor( global::Windows.Media.Core.IMediaStreamDescriptor descriptor,  global::System.Guid keyID,  byte[] proBytes)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Protection.PlayReady.NDStreamParserNotifier", "void NDStreamParserNotifier.OnBeginSetupDecryptor(IMediaStreamDescriptor descriptor, Guid keyID, byte[] proBytes)");
		}
		#endif
		// Processing: Windows.Media.Protection.PlayReady.INDStreamParserNotifier
	}
}
