#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RadialControllerRotationChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.RadialControllerScreenContact Contact
		{
			get
			{
				throw new global::System.NotImplementedException("The member RadialControllerScreenContact RadialControllerRotationChangedEventArgs.Contact is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double RotationDeltaInDegrees
		{
			get
			{
				throw new global::System.NotImplementedException("The member double RadialControllerRotationChangedEventArgs.RotationDeltaInDegrees is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsButtonPressed
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RadialControllerRotationChangedEventArgs.IsButtonPressed is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Haptics.SimpleHapticsController SimpleHapticsController
		{
			get
			{
				throw new global::System.NotImplementedException("The member SimpleHapticsController RadialControllerRotationChangedEventArgs.SimpleHapticsController is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.RadialControllerRotationChangedEventArgs.RotationDeltaInDegrees.get
		// Forced skipping of method Windows.UI.Input.RadialControllerRotationChangedEventArgs.Contact.get
		// Forced skipping of method Windows.UI.Input.RadialControllerRotationChangedEventArgs.IsButtonPressed.get
		// Forced skipping of method Windows.UI.Input.RadialControllerRotationChangedEventArgs.SimpleHapticsController.get
	}
}
