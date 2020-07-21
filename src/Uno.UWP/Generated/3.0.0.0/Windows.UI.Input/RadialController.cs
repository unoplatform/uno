#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RadialController 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool UseAutomaticHapticFeedback
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RadialController.UseAutomaticHapticFeedback is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "bool RadialController.UseAutomaticHapticFeedback");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double RotationResolutionInDegrees
		{
			get
			{
				throw new global::System.NotImplementedException("The member double RadialController.RotationResolutionInDegrees is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "double RadialController.RotationResolutionInDegrees");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.RadialControllerMenu Menu
		{
			get
			{
				throw new global::System.NotImplementedException("The member RadialControllerMenu RadialController.Menu is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Input.RadialController.Menu.get
		// Forced skipping of method Windows.UI.Input.RadialController.RotationResolutionInDegrees.get
		// Forced skipping of method Windows.UI.Input.RadialController.RotationResolutionInDegrees.set
		// Forced skipping of method Windows.UI.Input.RadialController.UseAutomaticHapticFeedback.get
		// Forced skipping of method Windows.UI.Input.RadialController.UseAutomaticHapticFeedback.set
		// Forced skipping of method Windows.UI.Input.RadialController.ScreenContactStarted.add
		// Forced skipping of method Windows.UI.Input.RadialController.ScreenContactStarted.remove
		// Forced skipping of method Windows.UI.Input.RadialController.ScreenContactEnded.add
		// Forced skipping of method Windows.UI.Input.RadialController.ScreenContactEnded.remove
		// Forced skipping of method Windows.UI.Input.RadialController.ScreenContactContinued.add
		// Forced skipping of method Windows.UI.Input.RadialController.ScreenContactContinued.remove
		// Forced skipping of method Windows.UI.Input.RadialController.ControlLost.add
		// Forced skipping of method Windows.UI.Input.RadialController.ControlLost.remove
		// Forced skipping of method Windows.UI.Input.RadialController.RotationChanged.add
		// Forced skipping of method Windows.UI.Input.RadialController.RotationChanged.remove
		// Forced skipping of method Windows.UI.Input.RadialController.ButtonClicked.add
		// Forced skipping of method Windows.UI.Input.RadialController.ButtonClicked.remove
		// Forced skipping of method Windows.UI.Input.RadialController.ControlAcquired.add
		// Forced skipping of method Windows.UI.Input.RadialController.ControlAcquired.remove
		// Forced skipping of method Windows.UI.Input.RadialController.ButtonPressed.add
		// Forced skipping of method Windows.UI.Input.RadialController.ButtonPressed.remove
		// Forced skipping of method Windows.UI.Input.RadialController.ButtonHolding.add
		// Forced skipping of method Windows.UI.Input.RadialController.ButtonHolding.remove
		// Forced skipping of method Windows.UI.Input.RadialController.ButtonReleased.add
		// Forced skipping of method Windows.UI.Input.RadialController.ButtonReleased.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool RadialController.IsSupported() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Input.RadialController CreateForCurrentView()
		{
			throw new global::System.NotImplementedException("The member RadialController RadialController.CreateForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, global::Windows.UI.Input.RadialControllerButtonClickedEventArgs> ButtonClicked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerButtonClickedEventArgs> RadialController.ButtonClicked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerButtonClickedEventArgs> RadialController.ButtonClicked");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, global::Windows.UI.Input.RadialControllerControlAcquiredEventArgs> ControlAcquired
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerControlAcquiredEventArgs> RadialController.ControlAcquired");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerControlAcquiredEventArgs> RadialController.ControlAcquired");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, object> ControlLost
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, object> RadialController.ControlLost");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, object> RadialController.ControlLost");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, global::Windows.UI.Input.RadialControllerRotationChangedEventArgs> RotationChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerRotationChangedEventArgs> RadialController.RotationChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerRotationChangedEventArgs> RadialController.RotationChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, global::Windows.UI.Input.RadialControllerScreenContactContinuedEventArgs> ScreenContactContinued
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerScreenContactContinuedEventArgs> RadialController.ScreenContactContinued");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerScreenContactContinuedEventArgs> RadialController.ScreenContactContinued");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, object> ScreenContactEnded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, object> RadialController.ScreenContactEnded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, object> RadialController.ScreenContactEnded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, global::Windows.UI.Input.RadialControllerScreenContactStartedEventArgs> ScreenContactStarted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerScreenContactStartedEventArgs> RadialController.ScreenContactStarted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerScreenContactStartedEventArgs> RadialController.ScreenContactStarted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, global::Windows.UI.Input.RadialControllerButtonHoldingEventArgs> ButtonHolding
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerButtonHoldingEventArgs> RadialController.ButtonHolding");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerButtonHoldingEventArgs> RadialController.ButtonHolding");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, global::Windows.UI.Input.RadialControllerButtonPressedEventArgs> ButtonPressed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerButtonPressedEventArgs> RadialController.ButtonPressed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerButtonPressedEventArgs> RadialController.ButtonPressed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Input.RadialController, global::Windows.UI.Input.RadialControllerButtonReleasedEventArgs> ButtonReleased
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerButtonReleasedEventArgs> RadialController.ButtonReleased");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Input.RadialController", "event TypedEventHandler<RadialController, RadialControllerButtonReleasedEventArgs> RadialController.ButtonReleased");
			}
		}
		#endif
	}
}
