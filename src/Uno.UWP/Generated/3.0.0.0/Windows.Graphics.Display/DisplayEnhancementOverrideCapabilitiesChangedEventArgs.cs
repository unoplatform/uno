#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayEnhancementOverrideCapabilitiesChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Graphics.Display.DisplayEnhancementOverrideCapabilities Capabilities
		{
			get
			{
				throw new global::System.NotImplementedException("The member DisplayEnhancementOverrideCapabilities DisplayEnhancementOverrideCapabilitiesChangedEventArgs.Capabilities is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DisplayEnhancementOverrideCapabilities%20DisplayEnhancementOverrideCapabilitiesChangedEventArgs.Capabilities");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverrideCapabilitiesChangedEventArgs.Capabilities.get
	}
}
