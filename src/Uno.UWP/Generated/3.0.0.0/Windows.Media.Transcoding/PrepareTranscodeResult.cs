#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Transcoding
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrepareTranscodeResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanTranscode
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PrepareTranscodeResult.CanTranscode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Transcoding.TranscodeFailureReason FailureReason
		{
			get
			{
				throw new global::System.NotImplementedException("The member TranscodeFailureReason PrepareTranscodeResult.FailureReason is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Transcoding.PrepareTranscodeResult.CanTranscode.get
		// Forced skipping of method Windows.Media.Transcoding.PrepareTranscodeResult.FailureReason.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncActionWithProgress<double> TranscodeAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncActionWithProgress<double> PrepareTranscodeResult.TranscodeAsync() is not implemented in Uno.");
		}
		#endif
	}
}
