#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ArcadeStick : global::Windows.Gaming.Input.IGameController,global::Windows.Gaming.Input.IGameControllerBatteryInfo
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.Headset Headset
		{
			get
			{
				throw new global::System.NotImplementedException("The member Headset ArcadeStick.Headset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsWireless
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ArcadeStick.IsWireless is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User ArcadeStick.User is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Gaming.Input.ArcadeStick> ArcadeSticks
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ArcadeStick> ArcadeStick.ArcadeSticks is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.GameControllerButtonLabel GetButtonLabel( global::Windows.Gaming.Input.ArcadeStickButtons button)
		{
			throw new global::System.NotImplementedException("The member GameControllerButtonLabel ArcadeStick.GetButtonLabel(ArcadeStickButtons button) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.ArcadeStickReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member ArcadeStickReading ArcadeStick.GetCurrentReading() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.HeadsetConnected.add
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.HeadsetConnected.remove
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.HeadsetDisconnected.add
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.HeadsetDisconnected.remove
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.UserChanged.add
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.UserChanged.remove
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.Headset.get
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.IsWireless.get
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Power.BatteryReport TryGetBatteryReport()
		{
			throw new global::System.NotImplementedException("The member BatteryReport ArcadeStick.TryGetBatteryReport() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Gaming.Input.ArcadeStick FromGameController( global::Windows.Gaming.Input.IGameController gameController)
		{
			throw new global::System.NotImplementedException("The member ArcadeStick ArcadeStick.FromGameController(IGameController gameController) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.ArcadeStickAdded.add
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.ArcadeStickAdded.remove
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.ArcadeStickRemoved.add
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.ArcadeStickRemoved.remove
		// Forced skipping of method Windows.Gaming.Input.ArcadeStick.ArcadeSticks.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Gaming.Input.IGameController, global::Windows.Gaming.Input.Headset> HeadsetConnected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event TypedEventHandler<IGameController, Headset> ArcadeStick.HeadsetConnected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event TypedEventHandler<IGameController, Headset> ArcadeStick.HeadsetConnected");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event TypedEventHandler<IGameController, Headset> ArcadeStick.HeadsetDisconnected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event TypedEventHandler<IGameController, Headset> ArcadeStick.HeadsetDisconnected");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event TypedEventHandler<IGameController, UserChangedEventArgs> ArcadeStick.UserChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event TypedEventHandler<IGameController, UserChangedEventArgs> ArcadeStick.UserChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Gaming.Input.ArcadeStick> ArcadeStickAdded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event EventHandler<ArcadeStick> ArcadeStick.ArcadeStickAdded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event EventHandler<ArcadeStick> ArcadeStick.ArcadeStickAdded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Gaming.Input.ArcadeStick> ArcadeStickRemoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event EventHandler<ArcadeStick> ArcadeStick.ArcadeStickRemoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.ArcadeStick", "event EventHandler<ArcadeStick> ArcadeStick.ArcadeStickRemoved");
			}
		}
		#endif
		// Processing: Windows.Gaming.Input.IGameController
		// Processing: Windows.Gaming.Input.IGameControllerBatteryInfo
	}
}
