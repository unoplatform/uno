#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaStreamSourceSampleRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaStreamSample Sample
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaStreamSample MediaStreamSourceSampleRequest.Sample is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaStreamSourceSampleRequest", "MediaStreamSample MediaStreamSourceSampleRequest.Sample");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.IMediaStreamDescriptor StreamDescriptor
		{
			get
			{
				throw new global::System.NotImplementedException("The member IMediaStreamDescriptor MediaStreamSourceSampleRequest.StreamDescriptor is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaStreamSourceSampleRequest.StreamDescriptor.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaStreamSourceSampleRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member MediaStreamSourceSampleRequestDeferral MediaStreamSourceSampleRequest.GetDeferral() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaStreamSourceSampleRequest.Sample.set
		// Forced skipping of method Windows.Media.Core.MediaStreamSourceSampleRequest.Sample.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportSampleProgress( uint progress)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaStreamSourceSampleRequest", "void MediaStreamSourceSampleRequest.ReportSampleProgress(uint progress)");
		}
		#endif
	}
}
