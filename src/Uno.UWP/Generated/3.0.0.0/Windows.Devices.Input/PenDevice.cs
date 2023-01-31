#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PenDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid PenId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid PenDevice.PenId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20PenDevice.PenId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Haptics.SimpleHapticsController SimpleHapticsController
		{
			get
			{
				throw new global::System.NotImplementedException("The member SimpleHapticsController PenDevice.SimpleHapticsController is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SimpleHapticsController%20PenDevice.SimpleHapticsController");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Input.PenDevice.PenId.get
		// Forced skipping of method Windows.Devices.Input.PenDevice.SimpleHapticsController.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Input.PenDevice GetFromPointerId( uint pointerId)
		{
			throw new global::System.NotImplementedException("The member PenDevice PenDevice.GetFromPointerId(uint pointerId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PenDevice%20PenDevice.GetFromPointerId%28uint%20pointerId%29");
		}
		#endif
	}
}
