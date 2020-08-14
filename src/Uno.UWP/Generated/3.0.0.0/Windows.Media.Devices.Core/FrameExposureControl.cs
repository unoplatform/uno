#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class FrameExposureControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? FrameExposureControl.Value is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.Core.FrameExposureControl", "TimeSpan? FrameExposureControl.Value");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Auto
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool FrameExposureControl.Auto is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.Core.FrameExposureControl", "bool FrameExposureControl.Auto");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.Core.FrameExposureControl.Auto.get
		// Forced skipping of method Windows.Media.Devices.Core.FrameExposureControl.Auto.set
		// Forced skipping of method Windows.Media.Devices.Core.FrameExposureControl.Value.get
		// Forced skipping of method Windows.Media.Devices.Core.FrameExposureControl.Value.set
	}
}
