#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Lights
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class LampAvailabilityChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsAvailable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool LampAvailabilityChangedEventArgs.IsAvailable is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20LampAvailabilityChangedEventArgs.IsAvailable");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Lights.LampAvailabilityChangedEventArgs.IsAvailable.get
	}
}
