#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FrameFlashCapabilities 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool PowerSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FrameFlashCapabilities.PowerSupported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20FrameFlashCapabilities.PowerSupported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool RedEyeReductionSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FrameFlashCapabilities.RedEyeReductionSupported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20FrameFlashCapabilities.RedEyeReductionSupported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FrameFlashCapabilities.Supported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20FrameFlashCapabilities.Supported");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.Core.FrameFlashCapabilities.Supported.get
		// Forced skipping of method Windows.Media.Devices.Core.FrameFlashCapabilities.RedEyeReductionSupported.get
		// Forced skipping of method Windows.Media.Devices.Core.FrameFlashCapabilities.PowerSupported.get
	}
}
