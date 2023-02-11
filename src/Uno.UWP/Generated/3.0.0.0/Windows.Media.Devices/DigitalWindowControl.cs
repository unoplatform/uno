#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DigitalWindowControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.DigitalWindowMode CurrentMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member DigitalWindowMode DigitalWindowControl.CurrentMode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DigitalWindowMode%20DigitalWindowControl.CurrentMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DigitalWindowControl.IsSupported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20DigitalWindowControl.IsSupported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Devices.DigitalWindowCapability> SupportedCapabilities
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<DigitalWindowCapability> DigitalWindowControl.SupportedCapabilities is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CDigitalWindowCapability%3E%20DigitalWindowControl.SupportedCapabilities");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.DigitalWindowMode[] SupportedModes
		{
			get
			{
				throw new global::System.NotImplementedException("The member DigitalWindowMode[] DigitalWindowControl.SupportedModes is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DigitalWindowMode%5B%5D%20DigitalWindowControl.SupportedModes");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.DigitalWindowControl.IsSupported.get
		// Forced skipping of method Windows.Media.Devices.DigitalWindowControl.SupportedModes.get
		// Forced skipping of method Windows.Media.Devices.DigitalWindowControl.CurrentMode.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.DigitalWindowBounds GetBounds()
		{
			throw new global::System.NotImplementedException("The member DigitalWindowBounds DigitalWindowControl.GetBounds() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DigitalWindowBounds%20DigitalWindowControl.GetBounds%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Configure( global::Windows.Media.Devices.DigitalWindowMode digitalWindowMode)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.DigitalWindowControl", "void DigitalWindowControl.Configure(DigitalWindowMode digitalWindowMode)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Configure( global::Windows.Media.Devices.DigitalWindowMode digitalWindowMode,  global::Windows.Media.Devices.DigitalWindowBounds digitalWindowBounds)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Devices.DigitalWindowControl", "void DigitalWindowControl.Configure(DigitalWindowMode digitalWindowMode, DigitalWindowBounds digitalWindowBounds)");
		}
		#endif
		// Forced skipping of method Windows.Media.Devices.DigitalWindowControl.SupportedCapabilities.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Devices.DigitalWindowCapability GetCapabilityForSize( int width,  int height)
		{
			throw new global::System.NotImplementedException("The member DigitalWindowCapability DigitalWindowControl.GetCapabilityForSize(int width, int height) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DigitalWindowCapability%20DigitalWindowControl.GetCapabilityForSize%28int%20width%2C%20int%20height%29");
		}
		#endif
	}
}
