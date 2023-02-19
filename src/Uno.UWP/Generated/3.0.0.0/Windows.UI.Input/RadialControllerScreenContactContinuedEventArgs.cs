#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RadialControllerScreenContactContinuedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.RadialControllerScreenContact Contact
		{
			get
			{
				throw new global::System.NotImplementedException("The member RadialControllerScreenContact RadialControllerScreenContactContinuedEventArgs.Contact is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=RadialControllerScreenContact%20RadialControllerScreenContactContinuedEventArgs.Contact");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsButtonPressed
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RadialControllerScreenContactContinuedEventArgs.IsButtonPressed is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20RadialControllerScreenContactContinuedEventArgs.IsButtonPressed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Haptics.SimpleHapticsController SimpleHapticsController
		{
			get
			{
				throw new global::System.NotImplementedException("The member SimpleHapticsController RadialControllerScreenContactContinuedEventArgs.SimpleHapticsController is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SimpleHapticsController%20RadialControllerScreenContactContinuedEventArgs.SimpleHapticsController");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.RadialControllerScreenContactContinuedEventArgs.Contact.get
		// Forced skipping of method Windows.UI.Input.RadialControllerScreenContactContinuedEventArgs.IsButtonPressed.get
		// Forced skipping of method Windows.UI.Input.RadialControllerScreenContactContinuedEventArgs.SimpleHapticsController.get
	}
}
