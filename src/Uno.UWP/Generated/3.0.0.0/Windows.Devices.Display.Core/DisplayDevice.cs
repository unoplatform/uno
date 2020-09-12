#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplaySource CreateScanoutSource( global::Windows.Devices.Display.Core.DisplayTarget target)
		{
			throw new global::System.NotImplementedException("The member DisplaySource DisplayDevice.CreateScanoutSource(DisplayTarget target) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplaySurface CreatePrimary( global::Windows.Devices.Display.Core.DisplayTarget target,  global::Windows.Devices.Display.Core.DisplayPrimaryDescription desc)
		{
			throw new global::System.NotImplementedException("The member DisplaySurface DisplayDevice.CreatePrimary(DisplayTarget target, DisplayPrimaryDescription desc) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayTaskPool CreateTaskPool()
		{
			throw new global::System.NotImplementedException("The member DisplayTaskPool DisplayDevice.CreateTaskPool() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayFence CreatePeriodicFence( global::Windows.Devices.Display.Core.DisplayTarget target,  global::System.TimeSpan offsetFromVBlank)
		{
			throw new global::System.NotImplementedException("The member DisplayFence DisplayDevice.CreatePeriodicFence(DisplayTarget target, TimeSpan offsetFromVBlank) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void WaitForVBlank( global::Windows.Devices.Display.Core.DisplaySource source)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Display.Core.DisplayDevice", "void DisplayDevice.WaitForVBlank(DisplaySource source)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayScanout CreateSimpleScanout( global::Windows.Devices.Display.Core.DisplaySource pSource,  global::Windows.Devices.Display.Core.DisplaySurface pSurface,  uint SubResourceIndex,  uint SyncInterval)
		{
			throw new global::System.NotImplementedException("The member DisplayScanout DisplayDevice.CreateSimpleScanout(DisplaySource pSource, DisplaySurface pSurface, uint SubResourceIndex, uint SyncInterval) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCapabilitySupported( global::Windows.Devices.Display.Core.DisplayDeviceCapability capability)
		{
			throw new global::System.NotImplementedException("The member bool DisplayDevice.IsCapabilitySupported(DisplayDeviceCapability capability) is not implemented in Uno.");
		}
		#endif
	}
}
