#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class OpticalImageStabilizationControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.OpticalImageStabilizationMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member OpticalImageStabilizationMode OpticalImageStabilizationControl.Mode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.OpticalImageStabilizationControl", "OpticalImageStabilizationMode OpticalImageStabilizationControl.Mode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Supported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool OpticalImageStabilizationControl.Supported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.OpticalImageStabilizationMode> SupportedModes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<OpticalImageStabilizationMode> OpticalImageStabilizationControl.SupportedModes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.OpticalImageStabilizationControl.Supported.get
		// Forced skipping of method Windows.Media.Devices.OpticalImageStabilizationControl.SupportedModes.get
		// Forced skipping of method Windows.Media.Devices.OpticalImageStabilizationControl.Mode.get
		// Forced skipping of method Windows.Media.Devices.OpticalImageStabilizationControl.Mode.set
	}
}
