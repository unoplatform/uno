#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UINavigationController : global::Windows.Gaming.Input.IGameController,global::Windows.Gaming.Input.IGameControllerBatteryInfo
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.Headset Headset
		{
			get
			{
				throw new global::System.NotImplementedException("The member Headset UINavigationController.Headset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsWireless
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool UINavigationController.IsWireless is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User UINavigationController.User is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Gaming.Input.UINavigationController> UINavigationControllers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UINavigationController> UINavigationController.UINavigationControllers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.UINavigationReading GetCurrentReading()
		{
			throw new global::System.NotImplementedException("The member UINavigationReading UINavigationController.GetCurrentReading() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.GameControllerButtonLabel GetOptionalButtonLabel( global::Windows.Gaming.Input.OptionalUINavigationButtons button)
		{
			throw new global::System.NotImplementedException("The member GameControllerButtonLabel UINavigationController.GetOptionalButtonLabel(OptionalUINavigationButtons button) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Gaming.Input.GameControllerButtonLabel GetRequiredButtonLabel( global::Windows.Gaming.Input.RequiredUINavigationButtons button)
		{
			throw new global::System.NotImplementedException("The member GameControllerButtonLabel UINavigationController.GetRequiredButtonLabel(RequiredUINavigationButtons button) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.HeadsetConnected.add
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.HeadsetConnected.remove
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.HeadsetDisconnected.add
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.HeadsetDisconnected.remove
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.UserChanged.add
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.UserChanged.remove
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.Headset.get
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.IsWireless.get
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.User.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Power.BatteryReport TryGetBatteryReport()
		{
			throw new global::System.NotImplementedException("The member BatteryReport UINavigationController.TryGetBatteryReport() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Gaming.Input.UINavigationController FromGameController( global::Windows.Gaming.Input.IGameController gameController)
		{
			throw new global::System.NotImplementedException("The member UINavigationController UINavigationController.FromGameController(IGameController gameController) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.UINavigationControllerAdded.add
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.UINavigationControllerAdded.remove
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.UINavigationControllerRemoved.add
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.UINavigationControllerRemoved.remove
		// Forced skipping of method Windows.Gaming.Input.UINavigationController.UINavigationControllers.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Gaming.Input.IGameController, global::Windows.Gaming.Input.Headset> HeadsetConnected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event TypedEventHandler<IGameController, Headset> UINavigationController.HeadsetConnected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event TypedEventHandler<IGameController, Headset> UINavigationController.HeadsetConnected");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event TypedEventHandler<IGameController, Headset> UINavigationController.HeadsetDisconnected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event TypedEventHandler<IGameController, Headset> UINavigationController.HeadsetDisconnected");
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
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event TypedEventHandler<IGameController, UserChangedEventArgs> UINavigationController.UserChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event TypedEventHandler<IGameController, UserChangedEventArgs> UINavigationController.UserChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Gaming.Input.UINavigationController> UINavigationControllerAdded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event EventHandler<UINavigationController> UINavigationController.UINavigationControllerAdded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event EventHandler<UINavigationController> UINavigationController.UINavigationControllerAdded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Gaming.Input.UINavigationController> UINavigationControllerRemoved
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event EventHandler<UINavigationController> UINavigationController.UINavigationControllerRemoved");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Gaming.Input.UINavigationController", "event EventHandler<UINavigationController> UINavigationController.UINavigationControllerRemoved");
			}
		}
		#endif
		// Processing: Windows.Gaming.Input.IGameController
		// Processing: Windows.Gaming.Input.IGameControllerBatteryInfo
	}
}
