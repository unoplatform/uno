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
			throw new global::System.NotImplementedException("The member DisplaySource DisplayDevice.CreateScanoutSource(DisplayTarget target) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplaySource%20DisplayDevice.CreateScanoutSource%28DisplayTarget%20target%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplaySurface CreatePrimary( global::Windows.Devices.Display.Core.DisplayTarget target,  global::Windows.Devices.Display.Core.DisplayPrimaryDescription desc)
		{
			throw new global::System.NotImplementedException("The member DisplaySurface DisplayDevice.CreatePrimary(DisplayTarget target, DisplayPrimaryDescription desc) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplaySurface%20DisplayDevice.CreatePrimary%28DisplayTarget%20target%2C%20DisplayPrimaryDescription%20desc%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayTaskPool CreateTaskPool()
		{
			throw new global::System.NotImplementedException("The member DisplayTaskPool DisplayDevice.CreateTaskPool() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplayTaskPool%20DisplayDevice.CreateTaskPool%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayFence CreatePeriodicFence( global::Windows.Devices.Display.Core.DisplayTarget target,  global::System.TimeSpan offsetFromVBlank)
		{
			throw new global::System.NotImplementedException("The member DisplayFence DisplayDevice.CreatePeriodicFence(DisplayTarget target, TimeSpan offsetFromVBlank) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplayFence%20DisplayDevice.CreatePeriodicFence%28DisplayTarget%20target%2C%20TimeSpan%20offsetFromVBlank%29");
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
			throw new global::System.NotImplementedException("The member DisplayScanout DisplayDevice.CreateSimpleScanout(DisplaySource pSource, DisplaySurface pSurface, uint SubResourceIndex, uint SyncInterval) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplayScanout%20DisplayDevice.CreateSimpleScanout%28DisplaySource%20pSource%2C%20DisplaySurface%20pSurface%2C%20uint%20SubResourceIndex%2C%20uint%20SyncInterval%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCapabilitySupported( global::Windows.Devices.Display.Core.DisplayDeviceCapability capability)
		{
			throw new global::System.NotImplementedException("The member bool DisplayDevice.IsCapabilitySupported(DisplayDeviceCapability capability) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20DisplayDevice.IsCapabilitySupported%28DisplayDeviceCapability%20capability%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayScanout CreateSimpleScanoutWithDirtyRectsAndOptions( global::Windows.Devices.Display.Core.DisplaySource source,  global::Windows.Devices.Display.Core.DisplaySurface surface,  uint subresourceIndex,  uint syncInterval,  global::System.Collections.Generic.IEnumerable<global::Windows.Graphics.RectInt32> dirtyRects,  global::Windows.Devices.Display.Core.DisplayScanoutOptions options)
		{
			throw new global::System.NotImplementedException("The member DisplayScanout DisplayDevice.CreateSimpleScanoutWithDirtyRectsAndOptions(DisplaySource source, DisplaySurface surface, uint subresourceIndex, uint syncInterval, IEnumerable<RectInt32> dirtyRects, DisplayScanoutOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplayScanout%20DisplayDevice.CreateSimpleScanoutWithDirtyRectsAndOptions%28DisplaySource%20source%2C%20DisplaySurface%20surface%2C%20uint%20subresourceIndex%2C%20uint%20syncInterval%2C%20IEnumerable%3CRectInt32%3E%20dirtyRects%2C%20DisplayScanoutOptions%20options%29");
		}
		#endif
	}
}
