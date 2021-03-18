#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MouseCapabilities 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int HorizontalWheelPresent
		{
			get
			{
				throw new global::System.NotImplementedException("The member int MouseCapabilities.HorizontalWheelPresent is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MousePresent
		{
			get
			{
				throw new global::System.NotImplementedException("The member int MouseCapabilities.MousePresent is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint NumberOfButtons
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint MouseCapabilities.NumberOfButtons is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int SwapButtons
		{
			get
			{
				throw new global::System.NotImplementedException("The member int MouseCapabilities.SwapButtons is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int VerticalWheelPresent
		{
			get
			{
				throw new global::System.NotImplementedException("The member int MouseCapabilities.VerticalWheelPresent is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MouseCapabilities() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Input.MouseCapabilities", "MouseCapabilities.MouseCapabilities()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Input.MouseCapabilities.MouseCapabilities()
		// Forced skipping of method Windows.Devices.Input.MouseCapabilities.MousePresent.get
		// Forced skipping of method Windows.Devices.Input.MouseCapabilities.VerticalWheelPresent.get
		// Forced skipping of method Windows.Devices.Input.MouseCapabilities.HorizontalWheelPresent.get
		// Forced skipping of method Windows.Devices.Input.MouseCapabilities.SwapButtons.get
		// Forced skipping of method Windows.Devices.Input.MouseCapabilities.NumberOfButtons.get
	}
}
