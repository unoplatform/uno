#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DeviceServicingDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Arguments
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DeviceServicingDetails.Arguments is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DeviceServicingDetails.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan ExpectedDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan DeviceServicingDetails.ExpectedDuration is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Background.DeviceServicingDetails.DeviceId.get
		// Forced skipping of method Windows.Devices.Background.DeviceServicingDetails.Arguments.get
		// Forced skipping of method Windows.Devices.Background.DeviceServicingDetails.ExpectedDuration.get
	}
}
