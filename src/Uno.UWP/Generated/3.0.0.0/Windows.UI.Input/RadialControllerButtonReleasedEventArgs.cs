#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RadialControllerButtonReleasedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.RadialControllerScreenContact Contact
		{
			get
			{
				throw new global::System.NotImplementedException("The member RadialControllerScreenContact RadialControllerButtonReleasedEventArgs.Contact is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Haptics.SimpleHapticsController SimpleHapticsController
		{
			get
			{
				throw new global::System.NotImplementedException("The member SimpleHapticsController RadialControllerButtonReleasedEventArgs.SimpleHapticsController is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.RadialControllerButtonReleasedEventArgs.Contact.get
		// Forced skipping of method Windows.UI.Input.RadialControllerButtonReleasedEventArgs.SimpleHapticsController.get
	}
}
