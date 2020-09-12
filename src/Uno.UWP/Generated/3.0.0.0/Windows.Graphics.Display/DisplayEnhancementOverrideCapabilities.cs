#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayEnhancementOverrideCapabilities 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsBrightnessControlSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayEnhancementOverrideCapabilities.IsBrightnessControlSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsBrightnessNitsControlSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayEnhancementOverrideCapabilities.IsBrightnessNitsControlSupported is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverrideCapabilities.IsBrightnessControlSupported.get
		// Forced skipping of method Windows.Graphics.Display.DisplayEnhancementOverrideCapabilities.IsBrightnessNitsControlSupported.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Graphics.Display.NitRange> GetSupportedNitRanges()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<NitRange> DisplayEnhancementOverrideCapabilities.GetSupportedNitRanges() is not implemented in Uno.");
		}
		#endif
	}
}
