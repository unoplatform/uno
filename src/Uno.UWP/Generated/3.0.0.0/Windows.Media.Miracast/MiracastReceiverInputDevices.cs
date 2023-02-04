#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverInputDevices 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverGameControllerDevice GameController
		{
			get
			{
				throw new global::System.NotImplementedException("The member MiracastReceiverGameControllerDevice MiracastReceiverInputDevices.GameController is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MiracastReceiverGameControllerDevice%20MiracastReceiverInputDevices.GameController");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverKeyboardDevice Keyboard
		{
			get
			{
				throw new global::System.NotImplementedException("The member MiracastReceiverKeyboardDevice MiracastReceiverInputDevices.Keyboard is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MiracastReceiverKeyboardDevice%20MiracastReceiverInputDevices.Keyboard");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverInputDevices.Keyboard.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverInputDevices.GameController.get
	}
}
