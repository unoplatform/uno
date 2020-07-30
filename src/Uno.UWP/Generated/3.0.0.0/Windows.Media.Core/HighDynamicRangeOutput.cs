#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HighDynamicRangeOutput 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double Certainty
		{
			get
			{
				throw new global::System.NotImplementedException("The member double HighDynamicRangeOutput.Certainty is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.Core.FrameController> FrameControllers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<FrameController> HighDynamicRangeOutput.FrameControllers is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.HighDynamicRangeOutput.Certainty.get
		// Forced skipping of method Windows.Media.Core.HighDynamicRangeOutput.FrameControllers.get
	}
}
