#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VariablePhotoCapturedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan CaptureTimeOffset
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan VariablePhotoCapturedEventArgs.CaptureTimeOffset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.CapturedFrameControlValues CapturedFrameControlValues
		{
			get
			{
				throw new global::System.NotImplementedException("The member CapturedFrameControlValues VariablePhotoCapturedEventArgs.CapturedFrameControlValues is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Capture.CapturedFrame Frame
		{
			get
			{
				throw new global::System.NotImplementedException("The member CapturedFrame VariablePhotoCapturedEventArgs.Frame is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint? UsedFrameControllerIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? VariablePhotoCapturedEventArgs.UsedFrameControllerIndex is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Core.VariablePhotoCapturedEventArgs.Frame.get
		// Forced skipping of method Windows.Media.Capture.Core.VariablePhotoCapturedEventArgs.CaptureTimeOffset.get
		// Forced skipping of method Windows.Media.Capture.Core.VariablePhotoCapturedEventArgs.UsedFrameControllerIndex.get
		// Forced skipping of method Windows.Media.Capture.Core.VariablePhotoCapturedEventArgs.CapturedFrameControlValues.get
	}
}
