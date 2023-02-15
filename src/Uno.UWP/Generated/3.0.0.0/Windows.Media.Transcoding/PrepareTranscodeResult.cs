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
				throw new global::System.NotImplementedException("The member bool PrepareTranscodeResult.CanTranscode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PrepareTranscodeResult.CanTranscode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Transcoding.TranscodeFailureReason FailureReason
		{
			get
			{
				throw new global::System.NotImplementedException("The member TranscodeFailureReason PrepareTranscodeResult.FailureReason is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TranscodeFailureReason%20PrepareTranscodeResult.FailureReason");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Transcoding.PrepareTranscodeResult.CanTranscode.get
		// Forced skipping of method Windows.Media.Transcoding.PrepareTranscodeResult.FailureReason.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncActionWithProgress<double> TranscodeAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncActionWithProgress<double> PrepareTranscodeResult.TranscodeAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncActionWithProgress%3Cdouble%3E%20PrepareTranscodeResult.TranscodeAsync%28%29");
		}
		#endif
	}
}
