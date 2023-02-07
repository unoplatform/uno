#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Gamepad : global::Windows.Gaming.Input.IGameController,global::Windows.Gaming.Input.IGameControllerBatteryInfo
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.Headset Headset
		{
			get
			{
				throw new global::System.NotImplementedException("The member Headset Gamepad.Headset is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Headset%20Gamepad.Headset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsWireless
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool Gamepad.IsWireless is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20Gamepad.IsWireless");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User Gamepad.User is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=User%20Gamepad.User");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.GamepadVibration Vibration
		{
			get
			{
				throw new global::System.NotImplementedException("The member GamepadVibration Gamepad.Vibration is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GamepadVibration%20Gamepad.Vibration");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "GamepadVibration Gamepad.Vibration");
			}
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Gaming.Input.Gamepad> Gamepads
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Gamepad> Gamepad.Gamepads is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CGamepad%3E%20Gamepad.Gamepads");
			}
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.Gamepad.Vibration.get
		// Forced skipping of method Windows.Gaming.Input.Gamepad.Vibration.set
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public  global::Windows.Gaming.Input.GamepadReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member GamepadReading Gamepad.GetCurrentReading() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GamepadReading%20Gamepad.GetCurrentReading%28%29");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.Gamepad.HeadsetConnected.add
		// Forced skipping of method Windows.Gaming.Input.Gamepad.HeadsetConnected.remove
		// Forced skipping of method Windows.Gaming.Input.Gamepad.HeadsetDisconnected.add
		// Forced skipping of method Windows.Gaming.Input.Gamepad.HeadsetDisconnected.remove
		// Forced skipping of method Windows.Gaming.Input.Gamepad.UserChanged.add
		// Forced skipping of method Windows.Gaming.Input.Gamepad.UserChanged.remove
		// Forced skipping of method Windows.Gaming.Input.Gamepad.Headset.get
		// Forced skipping of method Windows.Gaming.Input.Gamepad.IsWireless.get
		// Forced skipping of method Windows.Gaming.Input.Gamepad.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.GameControllerButtonLabel GetButtonLabel( global::Windows.Gaming.Input.GamepadButtons button)
		{
			throw new global::System.NotImplementedException("The member GameControllerButtonLabel Gamepad.GetButtonLabel(GamepadButtons button) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GameControllerButtonLabel%20Gamepad.GetButtonLabel%28GamepadButtons%20button%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Power.BatteryReport TryGetBatteryReport()
		{
			throw new global::System.NotImplementedException("The member BatteryReport Gamepad.TryGetBatteryReport() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BatteryReport%20Gamepad.TryGetBatteryReport%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Gaming.Input.Gamepad FromGameController( global::Windows.Gaming.Input.IGameController gameController)
		{
			throw new global::System.NotImplementedException("The member Gamepad Gamepad.FromGameController(IGameController gameController) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Gamepad%20Gamepad.FromGameController%28IGameController%20gameController%29");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.Gamepad.GamepadAdded.add
		// Forced skipping of method Windows.Gaming.Input.Gamepad.GamepadAdded.remove
		// Forced skipping of method Windows.Gaming.Input.Gamepad.GamepadRemoved.add
		// Forced skipping of method Windows.Gaming.Input.Gamepad.GamepadRemoved.remove
		// Forced skipping of method Windows.Gaming.Input.Gamepad.Gamepads.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Gaming.Input.IGameController, global::Windows.Gaming.Input.Headset> HeadsetConnected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event TypedEventHandler<IGameController, Headset> Gamepad.HeadsetConnected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event TypedEventHandler<IGameController, Headset> Gamepad.HeadsetConnected");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Gaming.Input.IGameController, global::Windows.Gaming.Input.Headset> HeadsetDisconnected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event TypedEventHandler<IGameController, Headset> Gamepad.HeadsetDisconnected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event TypedEventHandler<IGameController, Headset> Gamepad.HeadsetDisconnected");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Gaming.Input.IGameController, global::Windows.System.UserChangedEventArgs> UserChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event TypedEventHandler<IGameController, UserChangedEventArgs> Gamepad.UserChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event TypedEventHandler<IGameController, UserChangedEventArgs> Gamepad.UserChanged");
			}
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static event global::System.EventHandler<global::Windows.Gaming.Input.Gamepad> GamepadAdded
		{
			[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event EventHandler<Gamepad> Gamepad.GamepadAdded");
			}
			[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event EventHandler<Gamepad> Gamepad.GamepadAdded");
			}
		}
		#endif
		#if false || false || NET461 || false || __SKIA__ || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
		public static event global::System.EventHandler<global::Windows.Gaming.Input.Gamepad> GamepadRemoved
		{
			[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event EventHandler<Gamepad> Gamepad.GamepadRemoved");
			}
			[global::Uno.NotImplemented("NET461", "__SKIA__", "__NETSTD_REFERENCE__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.Gamepad", "event EventHandler<Gamepad> Gamepad.GamepadRemoved");
			}
		}
		#endif
		// Processing: Windows.Gaming.Input.IGameController
		// Processing: Windows.Gaming.Input.IGameControllerBatteryInfo
	}
}
