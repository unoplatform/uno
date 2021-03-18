#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayManagerPathsFailedOrInvalidatedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayManagerPathsFailedOrInvalidatedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Display.Core.DisplayManagerPathsFailedOrInvalidatedEventArgs", "bool DisplayManagerPathsFailedOrInvalidatedEventArgs.Handled");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayManagerPathsFailedOrInvalidatedEventArgs.Handled.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayManagerPathsFailedOrInvalidatedEventArgs.Handled.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral DisplayManagerPathsFailedOrInvalidatedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
